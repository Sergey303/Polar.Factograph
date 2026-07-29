using System.Runtime.CompilerServices;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectResourceSearchServiceTests
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

    [Fact]
    public async Task SearchByNameAsync_RanksExactPrefixAndTokenMatchesAndFiltersCassetteAccess()
    {
        FakeSearchStore searchStore = new(
            namesByKey: new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal)
            {
                ["ANN"] =
                [
                    Name("exact", "Ann", "en", "cass-a"),
                    Name("prefix", "Anna", "ru", "cass-a"),
                    Name("token", "Maria Ann", "ru", "cass-a"),
                    Name("hidden", "Ann Hidden", "ru", "cass-b")
                ]
            });
        FakeRdfStore rdfStore = new(
            [
                Head("exact", "cass-a"),
                Head("prefix", "cass-a"),
                Head("token", "cass-a"),
                Head("hidden", "cass-b")
            ],
            [
                TypeTriple("exact", "person", "cass-a"),
                TypeTriple("prefix", "person", "cass-a"),
                TypeTriple("token", "person", "cass-a")
            ]);
        ProjectResourceSearchService service = new(searchStore, rdfStore);

        IReadOnlyList<ProjectResourceSearchResult> results = await service.SearchByNameAsync(
            "ann",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.Equal(new[] { "exact", "prefix", "token" }, results.Select(result => result.ResourceId));
        Assert.Equal(new[] { 3, 2, 1 }, results.Select(result => result.Score));
        Assert.Equal("Ann", results[0].DisplayName);
        Assert.Equal("person", results[0].Type);
        Assert.DoesNotContain(results, result => result.ResourceId == "hidden");
        Assert.Contains("$system", searchStore.LastAllowedCassetteIds);
    }

    [Theory]
    [InlineData("Marchuk")]
    [InlineData("Vfhxer")]
    public async Task SearchByNameAsync_FindsCyrillicNameThroughAlternativeWriting(
        string query)
    {
        FakeSearchStore searchStore = new(
            namesByKey: new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal)
            {
                ["МАРЧУК"] = [Name("person-1", "Марчук", "ru", "cass-a")]
            });
        FakeRdfStore rdfStore = new(
            [Head("person-1", "cass-a")],
            [TypeTriple("person-1", "person", "cass-a")]);
        ProjectResourceSearchService service = new(searchStore, rdfStore);

        IReadOnlyList<ProjectResourceSearchResult> results = await service.SearchByNameAsync(
            query,
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        ProjectResourceSearchResult result = Assert.Single(results);
        Assert.Equal("person-1", result.ResourceId);
        Assert.Equal("Марчук", result.DisplayName);
        Assert.Equal(3, result.Score);
    }

    [Fact]
    public async Task SearchByNameAsync_PrefersOriginalWritingOverTransliteration()
    {
        FakeSearchStore searchStore = new(
            namesByKey: new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal)
            {
                ["MARCHUK"] = [Name("latin", "Marchuk", "en", "cass-a")],
                ["МАРЧУК"] = [Name("cyrillic", "Марчук", "ru", "cass-a")]
            });
        FakeRdfStore rdfStore = new(
            [Head("latin", "cass-a"), Head("cyrillic", "cass-a")],
            [
                TypeTriple("latin", "person", "cass-a"),
                TypeTriple("cyrillic", "person", "cass-a")
            ]);
        ProjectResourceSearchService service = new(searchStore, rdfStore);

        IReadOnlyList<ProjectResourceSearchResult> results = await service.SearchByNameAsync(
            "Marchuk",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.Equal(new[] { "latin", "cyrillic" }, results.Select(result => result.ResourceId));
        Assert.All(results, result => Assert.Equal(3, result.Score));
    }

    [Fact]
    public async Task SearchByWordsAsync_RanksDistinctMatchedWordsAndFallsBackToResourceId()
    {
        FakeSearchStore searchStore = new(
            wordsByWord: new Dictionary<string, IReadOnlyList<WordSearchHit>>(StringComparer.Ordinal)
            {
                ["ALPHA"] =
                [
                    Word("resource-1", "ALPHA", "Alpha beta", "cass-a"),
                    Word("resource-2", "ALPHA", "Alpha", "cass-a")
                ],
                ["BETA"] =
                [
                    Word("resource-1", "BETA", "Alpha beta", "cass-a")
                ]
            },
            namesByResource: new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal)
            {
                ["resource-2"] = [Name("resource-2", "Bob", "en", "cass-a")]
            });
        FakeRdfStore rdfStore = new(
            [Head("resource-1", "cass-a"), Head("resource-2", "cass-a")],
            Array.Empty<TripleRow>());
        ProjectResourceSearchService service = new(searchStore, rdfStore);

        IReadOnlyList<ProjectResourceSearchResult> results = await service.SearchByWordsAsync(
            "alpha beta alpha",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.Equal(new[] { "resource-1", "resource-2" }, results.Select(result => result.ResourceId));
        Assert.Equal(new[] { 2, 1 }, results.Select(result => result.Score));
        Assert.Equal("resource-1", results[0].DisplayName);
        Assert.Equal("Bob", results[1].DisplayName);
        Assert.Single(results[0].Matches);
    }

    [Fact]
    public async Task Search_RejectsUnboundedLimit()
    {
        ProjectResourceSearchService service = new(
            new FakeSearchStore(),
            new FakeRdfStore(Array.Empty<ResourceHead>(), Array.Empty<TripleRow>()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SearchByNameAsync(
            "a",
            new HashSet<string>(StringComparer.Ordinal),
            limit: 501));
    }

    private static NameSearchHit Name(
        string resourceId,
        string value,
        string? language,
        string cassetteId) => new(
        resourceId,
        LegacySearchIndexProjector.NamePredicate,
        value,
        language,
        cassetteId);

    private static WordSearchHit Word(
        string resourceId,
        string word,
        string value,
        string cassetteId) => new(
        resourceId,
        word,
        LegacySearchIndexProjector.DescriptionPredicate,
        value,
        Language: null,
        cassetteId);

    private static ResourceHead Head(string resourceId, string cassetteId) => new(
        resourceId,
        Guid.NewGuid(),
        cassetteId,
        cassetteId + ".fog",
        DateTimeOffset.UnixEpoch,
        IsDeleted: false);

    private static TripleRow TypeTriple(
        string resourceId,
        string type,
        string cassetteId) => new(
        Guid.NewGuid(),
        resourceId,
        RdfType,
        TripleObjectKind.Iri,
        type,
        Language: null,
        DataType: null,
        Guid.NewGuid(),
        cassetteId,
        cassetteId + ".fog",
        DateTimeOffset.UnixEpoch);

    private sealed class FakeSearchStore : IProjectSearchStore
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>> _namesByKey;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>> _namesByResource;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<WordSearchHit>> _wordsByWord;

        public FakeSearchStore(
            IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>>? namesByKey = null,
            IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>>? namesByResource = null,
            IReadOnlyDictionary<string, IReadOnlyList<WordSearchHit>>? wordsByWord = null)
        {
            _namesByKey = namesByKey
                ?? new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal);
            _namesByResource = namesByResource
                ?? new Dictionary<string, IReadOnlyList<NameSearchHit>>(StringComparer.Ordinal);
            _wordsByWord = wordsByWord
                ?? new Dictionary<string, IReadOnlyList<WordSearchHit>>(StringComparer.Ordinal);
        }

        public IReadOnlySet<string> LastAllowedCassetteIds { get; private set; } =
            new HashSet<string>(StringComparer.Ordinal);

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
            string normalizedSearchKey,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastAllowedCassetteIds = new HashSet<string>(allowedCassetteIds, StringComparer.Ordinal);
            IReadOnlyList<NameSearchHit> hits = _namesByKey.TryGetValue(
                normalizedSearchKey,
                out IReadOnlyList<NameSearchHit>? found)
                ? found
                : Array.Empty<NameSearchHit>();
            return Task.FromResult<IReadOnlyList<NameSearchHit>>(
                hits.Where(hit => allowedCassetteIds.Contains(hit.SourceCassetteId)).ToArray());
        }

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
            string resourceId,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<NameSearchHit> names = _namesByResource.TryGetValue(resourceId, out IReadOnlyList<NameSearchHit>? found)
                ? found
                : Array.Empty<NameSearchHit>();
            return Task.FromResult<IReadOnlyList<NameSearchHit>>(
                names.Where(hit => allowedCassetteIds.Contains(hit.SourceCassetteId)).ToArray());
        }

        public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
            string normalizedWord,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastAllowedCassetteIds = new HashSet<string>(allowedCassetteIds, StringComparer.Ordinal);
            IReadOnlyList<WordSearchHit> hits = _wordsByWord.TryGetValue(normalizedWord, out IReadOnlyList<WordSearchHit>? found)
                ? found
                : Array.Empty<WordSearchHit>();
            return Task.FromResult<IReadOnlyList<WordSearchHit>>(
                hits.Where(hit => allowedCassetteIds.Contains(hit.SourceCassetteId)).ToArray());
        }
    }

    private sealed class FakeRdfStore : IProjectRdfStore
    {
        private readonly Dictionary<string, ResourceHead> _heads;
        private readonly TripleRow[] _triples;

        public FakeRdfStore(
            IEnumerable<ResourceHead> heads,
            IEnumerable<TripleRow> triples)
        {
            _heads = heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
            _triples = triples.ToArray();
        }

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                _heads.TryGetValue(resourceId, out ResourceHead? head)
                    ? head
                    : null);
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
                    pattern.Subject is not null && !string.Equals(pattern.Subject, triple.Subject, StringComparison.Ordinal) ||
                    pattern.Predicate is not null && !string.Equals(pattern.Predicate, triple.Predicate, StringComparison.Ordinal) ||
                    pattern.ObjectKind is not null && pattern.ObjectKind != triple.ObjectKind ||
                    pattern.ObjectValue is not null && !string.Equals(pattern.ObjectValue, triple.ObjectValue, StringComparison.Ordinal))
                {
                    continue;
                }

                yield return triple;
            }
        }

        public Task RebuildAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
