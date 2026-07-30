using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class SemanticTimelineDateSelectionTests
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string O = "http://fogid.net/o/";
    private static readonly DateTimeOffset ModifiedAt =
        new(2026, 7, 30, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_UsesEarliestOntologyDateFromRelation()
    {
        await using TimelineFixture fixture = await TimelineFixture.CreateAsync(
            [Head("person-1"), Head("org-1"), Head("event-1")],
            [
                Iri("person-1", RdfType, O + "person"),
                Literal("person-1", O + "name", "Иван Иванов", "ru"),
                Iri("org-1", RdfType, O + "org-sys"),
                Literal("org-1", O + "name", "Институт", "ru"),
                Iri("event-1", RdfType, O + "event-relation"),
                Literal("event-1", O + "event-date", "1995", language: null),
                Literal("event-1", O + "event-date", "1985", language: null),
                Iri("event-1", O + "event-subject", "person-1"),
                Iri("event-1", O + "event-target", "org-1")
            ]);

        PresentedSemanticResourcePage? page = await fixture.Service.GetAsync(
            "person-1",
            fixture.Access);

        Assert.NotNull(page);
        SemanticResourceLink link = Assert.Single(page.RelatedResources);
        Assert.Equal("org-1", link.ResourceId);
        Assert.Equal("1985", link.DisplayDate);
        Assert.Equal("1985-01-01", link.SortDate);
    }

    [Fact]
    public async Task GetAsync_UsesMediaDateWhenRelationHasNoDate()
    {
        await using TimelineFixture fixture = await TimelineFixture.CreateAsync(
            [Head("person-1"), Head("photo-1"), Head("reflection-1")],
            [
                Iri("person-1", RdfType, O + "person"),
                Literal("person-1", O + "name", "Иван Иванов", "ru"),
                Iri("photo-1", RdfType, O + "photo-doc"),
                Literal("photo-1", O + "name", "Фотография конференции", "ru"),
                Literal("photo-1", O + "uri", "iiss://cassette/0001/0001", language: null),
                Literal("photo-1", O + "shot-date", "1978", language: null),
                Iri("reflection-1", RdfType, O + "reflection"),
                Iri("reflection-1", O + "reflected", "person-1"),
                Iri("reflection-1", O + "in-doc", "photo-1")
            ]);

        PresentedSemanticResourcePage? page = await fixture.Service.GetAsync(
            "person-1",
            fixture.Access);

        Assert.NotNull(page);
        SemanticPhotoCard photo = Assert.Single(page.Photos);
        Assert.Equal("1978", photo.DisplayDate);
        Assert.Equal("1978-01-01", photo.SortDate);
    }

    private static ResourceHead Head(string id) => new(
        id,
        Guid.NewGuid(),
        "cass",
        "source.fog",
        ModifiedAt,
        IsDeleted: false);

    private static TripleRow Iri(string subject, string predicate, string value) =>
        Triple(subject, predicate, TripleObjectKind.Iri, value, language: null);

    private static TripleRow Literal(
        string subject,
        string predicate,
        string value,
        string? language) =>
        Triple(subject, predicate, TripleObjectKind.Literal, value, language);

    private static TripleRow Triple(
        string subject,
        string predicate,
        TripleObjectKind kind,
        string value,
        string? language) => new(
        Guid.NewGuid(),
        subject,
        predicate,
        kind,
        value,
        language,
        DataType: null,
        Guid.NewGuid(),
        "cass",
        "source.fog",
        ModifiedAt);

    private sealed class TimelineFixture : IAsyncDisposable
    {
        private readonly string _directory;

        private TimelineFixture(
            string directory,
            SemanticResourcePageService service,
            ProjectAccessSnapshot access)
        {
            _directory = directory;
            Service = service;
            Access = access;
        }

        public SemanticResourcePageService Service { get; }
        public ProjectAccessSnapshot Access { get; }

        public static async Task<TimelineFixture> CreateAsync(
            ResourceHead[] heads,
            TripleRow[] triples)
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "polar-factograph-timeline-date-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string ontologyPath = Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                ontologyPath,
                OntologyXml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            OntologyCatalog ontology = await new XmlOntologyCatalogLoader()
                .LoadAsync(ontologyPath);
            TimelineStore store = new(heads, triples);
            ProjectResourcePortraitService portraits = new(store);
            ProjectResourceSearchService search = new(store, store, ontology);
            AuthorizedProjectReadService reads = new(portraits, search);
            SemanticResourcePageService service = new(
                reads,
                new OntologyResourcePortraitPresenter(ontology),
                ontology);

            ProjectAccessSnapshot access = new(
                "viewer",
                IsMember: true,
                new HashSet<string>([ProjectRights.Read, ProjectRights.Search], StringComparer.Ordinal),
                new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
                {
                    ["cass"] = new CassetteAccessSnapshot(
                        "cass",
                        Enabled: true,
                        AllowWrite: false,
                        new HashSet<string>([CassetteRights.Read], StringComparer.Ordinal))
                },
                DefaultWriteCassetteId: null);

            return new TimelineFixture(directory, service, access);
        }

        public ValueTask DisposeAsync()
        {
            Directory.Delete(_directory, recursive: true);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TimelineStore(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples) : IProjectRdfStore, IProjectSearchStore
    {
        private readonly Dictionary<string, ResourceHead> _heads =
            heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
        private readonly TripleRow[] _triples = triples.ToArray();

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                _heads.TryGetValue(resourceId, out ResourceHead? head) ? head : null);
        }

        public async IAsyncEnumerable<TripleRow> FindAsync(
            TriplePattern pattern,
            IReadOnlySet<string> allowedCassetteIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (TripleRow triple in _triples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!allowedCassetteIds.Contains(triple.SourceCassetteId) ||
                    pattern.Subject is not null && !string.Equals(
                        pattern.Subject,
                        triple.Subject,
                        StringComparison.Ordinal) ||
                    pattern.Predicate is not null && !string.Equals(
                        pattern.Predicate,
                        triple.Predicate,
                        StringComparison.Ordinal) ||
                    pattern.ObjectKind is not null && pattern.ObjectKind != triple.ObjectKind ||
                    pattern.ObjectValue is not null && !string.Equals(
                        pattern.ObjectValue,
                        triple.ObjectValue,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                yield return triple;
            }
        }

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
            string normalizedSearchKey,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
            string resourceId,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

        public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
            string normalizedWord,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WordSearchHit>>(Array.Empty<WordSearchHit>());

        public Task RebuildAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="http://fogid.net/o/entity" abstract="yes" />
          <Class rdf:about="http://fogid.net/o/sys-obj" abstract="yes">
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="http://fogid.net/o/person">
            <label xml:lang="ru">Персона</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="http://fogid.net/o/org-sys">
            <label xml:lang="ru">Организация</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="http://fogid.net/o/document">
            <label xml:lang="ru">Документ</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="http://fogid.net/o/photo-doc">
            <label xml:lang="ru">Фотодокумент</label>
            <SubClassOf rdf:resource="http://fogid.net/o/document" />
          </Class>
          <Class rdf:about="http://fogid.net/o/event-relation">
            <label xml:lang="ru">Событие</label>
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="http://fogid.net/o/reflection">
            <label xml:lang="ru">Отражение</label>
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <DatatypeProperty rdf:about="http://fogid.net/o/name">
            <label xml:lang="ru">имя</label>
            <domain rdf:resource="http://fogid.net/o/sys-obj" />
            <range rdf:resource="http://fogid.net/o/text" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/event-date">
            <label xml:lang="ru">дата события</label>
            <domain rdf:resource="http://fogid.net/o/event-relation" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/shot-date">
            <label xml:lang="ru">дата съёмки</label>
            <domain rdf:resource="http://fogid.net/o/document" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/uri">
            <domain rdf:resource="http://fogid.net/o/document" />
            <range rdf:resource="http://fogid.net/o/string" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/event-subject">
            <label xml:lang="ru">участник события</label>
            <inverse-label xml:lang="ru">событие</inverse-label>
            <domain rdf:resource="http://fogid.net/o/event-relation" />
            <range rdf:resource="http://fogid.net/o/person" />
          </ObjectProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/event-target">
            <label xml:lang="ru">место события</label>
            <domain rdf:resource="http://fogid.net/o/event-relation" />
            <range rdf:resource="http://fogid.net/o/org-sys" />
          </ObjectProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/reflected">
            <domain rdf:resource="http://fogid.net/o/reflection" />
            <range rdf:resource="http://fogid.net/o/sys-obj" />
          </ObjectProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/in-doc">
            <domain rdf:resource="http://fogid.net/o/reflection" />
            <range rdf:resource="http://fogid.net/o/document" />
          </ObjectProperty>
        </Ontology>
        """;
}
