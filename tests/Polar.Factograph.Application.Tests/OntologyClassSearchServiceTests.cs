using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class OntologyClassSearchServiceTests : IAsyncLifetime
{
    private const string RdfType =
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string O = "http://example.org/o/";
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "polar-factograph-type-search-tests",
        Guid.NewGuid().ToString("N"));
    private OntologyCatalog _ontology = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "ontology.xml");
        await File.WriteAllTextAsync(
            path,
            OntologyXml,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _ontology = await new XmlOntologyCatalogLoader().LoadAsync(path);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public void Suggest_PutsExactLocalizedClassBeforePrefixes()
    {
        OntologyClassSearchService service = new(
            new SearchStore([], [], []),
            new SearchStore([], [], []),
            _ontology);

        IReadOnlyList<OntologyClassSearchSuggestion> suggestions = service.Suggest(
            "  ОРГАНИЗАЦИЯ  ",
            preferredLanguage: "ru");

        Assert.NotEmpty(suggestions);
        Assert.Equal(O + "organization", suggestions[0].ClassId);
        Assert.Equal("Организация", suggestions[0].Label);
        Assert.True(suggestions[0].ExactMatch);
        Assert.Contains(suggestions, value => value.ClassId == O + "organization-event");
    }

    [Fact]
    public async Task SearchAsync_IncludesDescendantsAndFiltersInvisibleResources()
    {
        SearchStore store = new(
            [
                Head("org-direct", "public"),
                Head("institute", "public"),
                Head("person", "public"),
                Head("hidden-org", "hidden"),
                Head("deleted-org", "public", deleted: true)
            ],
            [
                Type("org-direct", O + "organization", "public"),
                Type("institute", O + "institute", "public"),
                Type("person", O + "person", "public"),
                Type("hidden-org", O + "organization", "hidden"),
                Type("deleted-org", O + "organization", "public")
            ],
            [
                Name("org-direct", "Бета", "public"),
                Name("institute", "Альфа", "public"),
                Name("person", "Персона", "public"),
                Name("hidden-org", "Скрытая", "hidden"),
                Name("deleted-org", "Удалённая", "public")
            ]);
        OntologyClassSearchService service = new(store, store, _ontology);

        ProjectResourceTypeSearchPage page = await service.SearchAsync(
            O + "organization",
            Access(),
            offset: 0,
            limit: 10,
            preferredLanguage: "ru");

        Assert.Equal("Организация", page.Label);
        Assert.Equal(2, page.Total);
        Assert.Collection(
            page.Results,
            first =>
            {
                Assert.Equal("institute", first.ResourceId);
                Assert.Equal("Альфа", first.DisplayName);
                Assert.Equal(O + "institute", first.Type);
            },
            second =>
            {
                Assert.Equal("org-direct", second.ResourceId);
                Assert.Equal("Бета", second.DisplayName);
                Assert.Equal(O + "organization", second.Type);
            });
        Assert.All(store.TypeEnumerationPatterns, pattern =>
        {
            Assert.Equal(RdfType, pattern.Predicate);
            Assert.Equal(TripleObjectKind.Iri, pattern.ObjectKind);
            Assert.NotNull(pattern.ObjectValue);
        });
        Assert.Contains(
            store.TypeEnumerationPatterns,
            pattern => pattern.ObjectValue == O + "organization");
        Assert.Contains(
            store.TypeEnumerationPatterns,
            pattern => pattern.ObjectValue == O + "institute");
    }

    [Fact]
    public async Task SearchAsync_PaginatesAfterStableNameOrdering()
    {
        SearchStore store = new(
            [Head("a", "public"), Head("b", "public"), Head("c", "public")],
            [
                Type("a", O + "organization", "public"),
                Type("b", O + "organization", "public"),
                Type("c", O + "organization", "public")
            ],
            [
                Name("a", "Альфа", "public"),
                Name("b", "Бета", "public"),
                Name("c", "Гамма", "public")
            ]);
        OntologyClassSearchService service = new(store, store, _ontology);

        ProjectResourceTypeSearchPage page = await service.SearchAsync(
            O + "organization",
            Access(),
            offset: 1,
            limit: 1);

        Assert.Equal(3, page.Total);
        ProjectResourceSearchResult result = Assert.Single(page.Results);
        Assert.Equal("b", result.ResourceId);
        Assert.Equal("Бета", result.DisplayName);
    }

    [Fact]
    public async Task SearchAsync_ReusesResolvedCategoryAcrossPages()
    {
        SearchStore store = new(
            [Head("a", "public"), Head("b", "public")],
            [
                Type("a", O + "organization", "public"),
                Type("b", O + "institute", "public")
            ],
            [Name("a", "Альфа", "public"), Name("b", "Бета", "public")]);
        OntologyClassSearchService service = new(store, store, _ontology);

        _ = await service.SearchAsync(O + "organization", Access(), offset: 0, limit: 1);
        int firstEnumerationCount = store.TypeEnumerationPatterns.Count;
        _ = await service.SearchAsync(O + "organization", Access(), offset: 1, limit: 1);

        Assert.True(firstEnumerationCount > 0);
        Assert.Equal(firstEnumerationCount, store.TypeEnumerationPatterns.Count);
    }

    private static ProjectAccessSnapshot Access() => new(
        "viewer",
        IsMember: true,
        new HashSet<string>([ProjectRights.Read, ProjectRights.Search], StringComparer.Ordinal),
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["public"] = new CassetteAccessSnapshot(
                "public",
                Enabled: true,
                AllowWrite: false,
                new HashSet<string>([CassetteRights.Read], StringComparer.Ordinal)),
            ["hidden"] = new CassetteAccessSnapshot(
                "hidden",
                Enabled: true,
                AllowWrite: false,
                new HashSet<string>(StringComparer.Ordinal))
        },
        DefaultWriteCassetteId: null);

    private static ResourceHead Head(
        string resourceId,
        string cassetteId,
        bool deleted = false) => new(
        resourceId,
        Guid.NewGuid(),
        cassetteId,
        "source.fog",
        DateTimeOffset.UnixEpoch,
        deleted);

    private static TripleRow Type(
        string subject,
        string type,
        string cassetteId) => new(
        Guid.NewGuid(),
        subject,
        RdfType,
        TripleObjectKind.Iri,
        type,
        null,
        null,
        Guid.NewGuid(),
        cassetteId,
        "source.fog",
        DateTimeOffset.UnixEpoch);

    private static NameSearchHit Name(
        string resourceId,
        string value,
        string cassetteId) => new(
        resourceId,
        "http://fogid.net/o/name",
        value,
        "ru",
        cassetteId);

    private sealed class SearchStore(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples,
        IEnumerable<NameSearchHit> names) : IProjectRdfStore, IProjectSearchStore
    {
        private readonly Dictionary<string, ResourceHead> _heads =
            heads.ToDictionary(value => value.ResourceId, StringComparer.Ordinal);
        private readonly TripleRow[] _triples = triples.ToArray();
        private readonly NameSearchHit[] _names = names.ToArray();

        public List<TriplePattern> TypeEnumerationPatterns { get; } = [];

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
            if (pattern.Predicate == RdfType &&
                pattern.ObjectKind == TripleObjectKind.Iri &&
                pattern.ObjectValue is not null)
            {
                TypeEnumerationPatterns.Add(pattern);
            }

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
            string resourceId,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(_names
                .Where(value => value.ResourceId == resourceId &&
                    allowedCassetteIds.Contains(value.SourceCassetteId))
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
          <Class rdf:about="http://example.org/o/entity" abstract="yes" />
          <Class rdf:about="http://example.org/o/organization" abstract="yes">
            <label xml:lang="ru">Организация</label>
            <SubClassOf rdf:resource="http://example.org/o/entity" />
          </Class>
          <Class rdf:about="http://example.org/o/institute">
            <label xml:lang="ru">Институт</label>
            <SubClassOf rdf:resource="http://example.org/o/organization" />
          </Class>
          <Class rdf:about="http://example.org/o/organization-event">
            <label xml:lang="ru">Организация событий</label>
            <SubClassOf rdf:resource="http://example.org/o/entity" />
          </Class>
          <Class rdf:about="http://example.org/o/person">
            <label xml:lang="ru">Персона</label>
            <SubClassOf rdf:resource="http://example.org/o/entity" />
          </Class>
        </Ontology>
        """;
}
