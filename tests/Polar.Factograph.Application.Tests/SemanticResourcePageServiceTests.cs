using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class SemanticResourcePageServiceTests
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string O = "http://fogid.net/o/";
    private static readonly DateTimeOffset ModifiedAt =
        new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ExpandsPersonPhotosOrganizationsAndCollectionsWithoutBridgeLinks()
    {
        await using SemanticPageFixture fixture = await SemanticPageFixture.CreateAsync();

        PresentedSemanticResourcePage? page = await fixture.Service.GetAsync(
            "person-1",
            fixture.Access);

        Assert.NotNull(page);
        Assert.Equal("person-1", page.Portrait.ResourceId);

        SemanticPhotoCard photo = Assert.Single(page.Photos);
        Assert.Equal("photo-1", photo.ResourceId);
        Assert.Equal("iiss://SypCassete@iis.nsk.su/0001/0001", photo.DocumentUri);
        Assert.Equal("org-1", photo.ContextResourceId);
        Assert.Equal("Институт систем информатики", photo.ContextLabel);

        SemanticResourceLink organization = Assert.Single(page.Organizations);
        Assert.Equal("org-1", organization.ResourceId);
        Assert.Equal("исследователь", organization.RelationLabel);

        SemanticResourceLink collection = Assert.Single(page.Collections);
        Assert.Equal("collection-1", collection.ResourceId);
        Assert.Equal("ЛШЮП — фотографии", collection.DisplayName);

        Assert.DoesNotContain(page.RelatedResources, link =>
            link.ResourceId is "reflection-person" or "reflection-org" or
                "participation-1" or "collection-member-1");
    }

    [Fact]
    public async Task GetAsync_ExpandsOrganizationParticipantsAndUsesOrganizationAsPhotoContext()
    {
        await using SemanticPageFixture fixture = await SemanticPageFixture.CreateAsync();

        PresentedSemanticResourcePage? page = await fixture.Service.GetAsync(
            "org-1",
            fixture.Access);

        Assert.NotNull(page);
        SemanticResourceLink participant = Assert.Single(page.Participants);
        Assert.Equal("person-1", participant.ResourceId);
        Assert.Equal("исследователь", participant.RelationLabel);

        SemanticPhotoCard photo = Assert.Single(page.Photos);
        Assert.Equal("photo-1", photo.ResourceId);
        Assert.Equal("org-1", photo.ContextResourceId);
        Assert.DoesNotContain(page.RelatedResources, link =>
            link.ResourceId is "reflection-org" or "participation-1");
    }

    [Fact]
    public async Task GetAsync_ExpandsCollectionPhotoMembersAndCanonicalizesBridgeAddress()
    {
        await using SemanticPageFixture fixture = await SemanticPageFixture.CreateAsync();

        PresentedSemanticResourcePage? collection = await fixture.Service.GetAsync(
            "collection-1",
            fixture.Access);
        PresentedSemanticResourcePage? bridged = await fixture.Service.GetAsync(
            "collection-member-1",
            fixture.Access);

        Assert.NotNull(collection);
        SemanticPhotoCard photo = Assert.Single(collection.Photos);
        Assert.Equal("photo-1", photo.ResourceId);
        Assert.Equal("collection-1", photo.ContextResourceId);

        Assert.NotNull(bridged);
        Assert.Equal("collection-member-1", bridged.RequestedResourceId);
        Assert.Equal("photo-1", bridged.Portrait.ResourceId);
    }

    private sealed class SemanticPageFixture : IAsyncDisposable
    {
        private readonly string _directory;

        private SemanticPageFixture(
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

        public static async Task<SemanticPageFixture> CreateAsync()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "polar-factograph-semantic-page-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string ontologyPath = Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                ontologyPath,
                OntologyXml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            OntologyCatalog ontology = await new XmlOntologyCatalogLoader()
                .LoadAsync(ontologyPath);
            SemanticStore store = new(CreateHeads(), CreateTriples());
            ProjectResourcePortraitService portraits = new(store);
            ProjectResourceSearchService search = new(store, store, ontology);
            AuthorizedProjectReadService reads = new(portraits, search);
            OntologyResourcePortraitPresenter presenter = new(ontology);
            SemanticResourcePageService service = new(reads, presenter, ontology);

            HashSet<string> projectRights = new(StringComparer.Ordinal)
            {
                ProjectRights.Read,
                ProjectRights.Search
            };
            HashSet<string> cassetteRights = new(StringComparer.Ordinal)
            {
                CassetteRights.Read
            };
            Dictionary<string, CassetteAccessSnapshot> cassettes = new(StringComparer.Ordinal)
            {
                ["cass"] = new CassetteAccessSnapshot(
                    "cass",
                    Enabled: true,
                    AllowWrite: false,
                    cassetteRights)
            };
            ProjectAccessSnapshot access = new(
                "user-1",
                IsMember: true,
                projectRights,
                cassettes,
                DefaultWriteCassetteId: null);

            return new SemanticPageFixture(directory, service, access);
        }

        public ValueTask DisposeAsync()
        {
            Directory.Delete(_directory, recursive: true);
            return ValueTask.CompletedTask;
        }

        private static ResourceHead[] CreateHeads() =>
        [
            Head("person-1"),
            Head("org-1"),
            Head("photo-1"),
            Head("collection-1"),
            Head("reflection-person"),
            Head("reflection-org"),
            Head("participation-1"),
            Head("collection-member-1")
        ];

        private static TripleRow[] CreateTriples() =>
        [
            Iri("person-1", RdfType, O + "person"),
            Literal("person-1", O + "name", "Марчук Александр Гурьевич", "ru"),

            Iri("org-1", RdfType, O + "org-sys"),
            Literal("org-1", O + "name", "Институт систем информатики", "ru"),

            Iri("photo-1", RdfType, O + "photo-doc"),
            Literal("photo-1", O + "name", "ЛШЮП, общая фотография", "ru"),
            Literal(
                "photo-1",
                O + "uri",
                "iiss://SypCassete@iis.nsk.su/0001/0001",
                language: null),

            Iri("collection-1", RdfType, O + "collection"),
            Literal("collection-1", O + "name", "ЛШЮП — фотографии", "ru"),

            Iri("reflection-person", RdfType, O + "reflection"),
            Iri("reflection-person", O + "reflected", "person-1"),
            Iri("reflection-person", O + "in-doc", "photo-1"),

            Iri("reflection-org", RdfType, O + "reflection"),
            Iri("reflection-org", O + "reflected", "org-1"),
            Iri("reflection-org", O + "in-doc", "photo-1"),

            Iri("participation-1", RdfType, O + "participation"),
            Iri("participation-1", O + "participant", "person-1"),
            Iri("participation-1", O + "in-org", "org-1"),
            Literal("participation-1", O + "role", "исследователь", "ru"),

            Iri("collection-member-1", RdfType, O + "collection-member"),
            Iri("collection-member-1", O + "in-collection", "collection-1"),
            Iri("collection-member-1", O + "collection-item", "photo-1")
        ];

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
              <Class rdf:about="http://fogid.net/o/collection">
                <label xml:lang="ru">Коллекция</label>
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
              <Class rdf:about="http://fogid.net/o/reflection">
                <SubClassOf rdf:resource="http://fogid.net/o/entity" />
              </Class>
              <Class rdf:about="http://fogid.net/o/participation">
                <SubClassOf rdf:resource="http://fogid.net/o/entity" />
              </Class>
              <Class rdf:about="http://fogid.net/o/collection-member">
                <SubClassOf rdf:resource="http://fogid.net/o/entity" />
              </Class>
              <DatatypeProperty rdf:about="http://fogid.net/o/name">
                <label xml:lang="ru">имя</label>
                <domain rdf:resource="http://fogid.net/o/sys-obj" />
                <range rdf:resource="http://fogid.net/o/text" />
              </DatatypeProperty>
              <DatatypeProperty rdf:about="http://fogid.net/o/uri">
                <domain rdf:resource="http://fogid.net/o/document" />
                <range rdf:resource="http://fogid.net/o/string" />
              </DatatypeProperty>
              <DatatypeProperty rdf:about="http://fogid.net/o/role">
                <domain rdf:resource="http://fogid.net/o/participation" />
                <range rdf:resource="http://fogid.net/o/text" />
              </DatatypeProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/reflected">
                <domain rdf:resource="http://fogid.net/o/reflection" />
                <range rdf:resource="http://fogid.net/o/sys-obj" />
              </ObjectProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/in-doc">
                <domain rdf:resource="http://fogid.net/o/reflection" />
                <range rdf:resource="http://fogid.net/o/document" />
              </ObjectProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/participant">
                <domain rdf:resource="http://fogid.net/o/participation" />
                <range rdf:resource="http://fogid.net/o/sys-obj" />
              </ObjectProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/in-org">
                <domain rdf:resource="http://fogid.net/o/participation" />
                <range rdf:resource="http://fogid.net/o/org-sys" />
              </ObjectProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/in-collection">
                <domain rdf:resource="http://fogid.net/o/collection-member" />
                <range rdf:resource="http://fogid.net/o/collection" />
              </ObjectProperty>
              <ObjectProperty rdf:about="http://fogid.net/o/collection-item">
                <domain rdf:resource="http://fogid.net/o/collection-member" />
                <range rdf:resource="http://fogid.net/o/sys-obj" />
              </ObjectProperty>
            </Ontology>
            """;
    }

    private sealed class SemanticStore(
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
}
