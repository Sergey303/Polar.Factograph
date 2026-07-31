using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectPotentialDuplicateServiceTests : IDisposable
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string O = "http://fogid.net/o/";
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"factograph-duplicate-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task FindAsync_FindsExactValueOfTheSameStringPredicateAndType()
    {
        TestContext context = await CreateContextAsync(
            [
                Head("person-1"),
                Head("organization-1")
            ],
            [
                Iri("person-1", RdfType, O + "person"),
                Literal("person-1", O + "name", "Иван Иванов", "ru"),
                Literal("person-1", O + "code", "A-42", null),
                Iri("organization-1", RdfType, O + "organization"),
                Literal("organization-1", O + "name", "Другая запись", "ru"),
                Literal("organization-1", O + "code", "A-42", null)
            ]);

        IReadOnlyList<PotentialDuplicateResource> results = await context.Service.FindAsync(
            O + "person",
            O + "code",
            "A-42",
            context.Access);

        PotentialDuplicateResource result = Assert.Single(results);
        Assert.Equal("person-1", result.ResourceId);
        Assert.Equal("Иван Иванов", result.DisplayName);
        Assert.False(result.AlternativeWriting);
    }

    [Fact]
    public async Task FindAsync_FindsNameThroughTransliteration()
    {
        TestContext context = await CreateContextAsync(
            [Head("person-1")],
            [
                Iri("person-1", RdfType, O + "person"),
                Literal("person-1", O + "name", "Марчук", "ru")
            ],
            namesByKey: new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal)
            {
                ["МАРЧУК"] =
                [
                    new NameSearchHit(
                        "person-1",
                        O + "name",
                        "Марчук",
                        "ru",
                        "cass")
                ]
            });

        IReadOnlyList<PotentialDuplicateResource> results = await context.Service.FindAsync(
            O + "person",
            O + "name",
            "Marchuk",
            context.Access);

        PotentialDuplicateResource result = Assert.Single(results);
        Assert.Equal("person-1", result.ResourceId);
        Assert.True(result.AlternativeWriting);
        Assert.Equal("Марчук", result.MatchedValue);
    }

    [Fact]
    public async Task FindAsync_IgnoresDateProperties()
    {
        TestContext context = await CreateContextAsync(
            [Head("person-1")],
            [
                Iri("person-1", RdfType, O + "person"),
                Literal("person-1", O + "date", "1987", null)
            ]);

        IReadOnlyList<PotentialDuplicateResource> results = await context.Service.FindAsync(
            O + "person",
            O + "date",
            "1987",
            context.Access);

        Assert.Empty(results);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<TestContext> CreateContextAsync(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples,
        IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>>? namesByKey = null)
    {
        Directory.CreateDirectory(_root);
        string ontologyPath = Path.Combine(_root, "ontology.xml");
        await File.WriteAllTextAsync(
            ontologyPath,
            OntologyXml,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        OntologyCatalog ontology = await new XmlOntologyCatalogLoader().LoadAsync(ontologyPath);
        TestStore store = new(heads, triples, namesByKey);
        ProjectResourcePortraitService portraits = new(store);
        ProjectResourceSearchService search = new(store, store, ontology);
        AuthorizedProjectReadService reads = new(portraits, search);
        ProjectPotentialDuplicateService service = new(store, reads, ontology);
        return new TestContext(service, CreateAccess());
    }

    private static ProjectAccessSnapshot CreateAccess() => new(
        "user",
        IsMember: true,
        new HashSet<string>(StringComparer.Ordinal)
        {
            ProjectRights.Read,
            ProjectRights.Search
        },
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cass"] = new CassetteAccessSnapshot(
                "cass",
                Enabled: true,
                AllowWrite: false,
                new HashSet<string>(StringComparer.Ordinal) { CassetteRights.Read })
        },
        DefaultWriteCassetteId: null);

    private static ResourceHead Head(string resourceId) => new(
        resourceId,
        Guid.NewGuid(),
        "cass",
        "source.fog",
        DateTimeOffset.UnixEpoch,
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
            DateTimeOffset.UnixEpoch);

    private sealed record TestContext(
        ProjectPotentialDuplicateService Service,
        ProjectAccessSnapshot Access);

    private sealed class TestStore(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples,
        IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>>? namesByKey)
        : IProjectRdfStore, IProjectSearchStore
    {
        private readonly Dictionary<string, ResourceHead> _heads =
            heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
        private readonly TripleRow[] _triples = triples.ToArray();
        private readonly IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>> _namesByKey =
            namesByKey ?? new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal);

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_heads.TryGetValue(resourceId, out ResourceHead? head)
                ? head
                : null);

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
                    pattern.Subject is not null && pattern.Subject != triple.Subject ||
                    pattern.Predicate is not null && pattern.Predicate != triple.Predicate ||
                    pattern.ObjectKind is not null && pattern.ObjectKind != triple.ObjectKind ||
                    pattern.ObjectValue is not null && pattern.ObjectValue != triple.ObjectValue)
                {
                    continue;
                }
                yield return triple;
            }
        }

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
            string normalizedSearchKey,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<NameSearchHit> hits = _namesByKey.TryGetValue(
                normalizedSearchKey,
                out IReadOnlyList<NameSearchHit>? value)
                ? value
                : Array.Empty<NameSearchHit>();
            return Task.FromResult<IReadOnlyList<NameSearchHit>>(
                hits.Where(hit => allowedCassetteIds.Contains(hit.SourceCassetteId)).ToArray());
        }

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
            string resourceId,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(
                _triples
                    .Where(triple =>
                        triple.Subject == resourceId &&
                        triple.Predicate == O + "name" &&
                        triple.ObjectKind == TripleObjectKind.Literal)
                    .Select(triple => new NameSearchHit(
                        triple.Subject,
                        triple.Predicate,
                        triple.ObjectValue,
                        triple.Language,
                        triple.SourceCassetteId))
                    .ToArray());

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
          <Class rdf:about="http://fogid.net/o/organization">
            <label xml:lang="ru">Организация</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <DatatypeProperty rdf:about="http://fogid.net/o/name">
            <label xml:lang="ru">имя</label>
            <domain rdf:resource="http://fogid.net/o/sys-obj" />
            <range rdf:resource="http://fogid.net/o/text" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/code">
            <label xml:lang="ru">код</label>
            <domain rdf:resource="http://fogid.net/o/sys-obj" />
            <range rdf:resource="http://fogid.net/o/string" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/date">
            <label xml:lang="ru">дата</label>
            <domain rdf:resource="http://fogid.net/o/entity" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
        </Ontology>
        """;
}
