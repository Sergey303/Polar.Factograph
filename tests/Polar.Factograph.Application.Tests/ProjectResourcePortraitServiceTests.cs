using System.Runtime.CompilerServices;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectResourcePortraitServiceTests
{
    private static readonly DateTimeOffset ModifiedAt = new(2026, 7, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid SourceRecordId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetAsync_ReturnsFieldsLinksInverseLinksAndProvenance()
    {
        FakeProjectRdfStore store = new(
            new[]
            {
                new ResourceHead(
                    "person-1",
                    SourceRecordId,
                    "cass-a",
                    "a.fog",
                    ModifiedAt,
                    IsDeleted: false)
            },
            new[]
            {
                Triple("person-1", RdfType, TripleObjectKind.Iri, "http://fogid.net/o/person", "cass-a"),
                Triple("person-1", "http://fogid.net/o/name", TripleObjectKind.Literal, "Иван", "cass-a", language: "ru"),
                Triple("person-1", "http://fogid.net/o/works-at", TripleObjectKind.Iri, "org-1", "cass-a"),
                Triple("photo-1", "http://fogid.net/o/depicts", TripleObjectKind.Iri, "person-1", "cass-b"),
                Triple("hidden-1", "http://fogid.net/o/mentions", TripleObjectKind.Iri, "person-1", "cass-c")
            });
        ProjectResourcePortraitService service = new(store);
        HashSet<string> allowed = new(StringComparer.Ordinal) { "cass-a", "cass-b" };

        ProjectResourcePortrait? portrait = await service.GetAsync("person-1", allowed);

        Assert.NotNull(portrait);
        Assert.Equal("http://fogid.net/o/person", portrait.Type);

        ResourceLiteralField literal = Assert.Single(portrait.Literals);
        Assert.Equal("http://fogid.net/o/name", literal.Predicate);
        Assert.Equal("Иван", literal.Value);
        Assert.Equal("ru", literal.Language);

        ResourceDirectLink direct = Assert.Single(portrait.DirectLinks);
        Assert.Equal("http://fogid.net/o/works-at", direct.Predicate);
        Assert.Equal("org-1", direct.TargetResourceId);

        ResourceInverseLink inverse = Assert.Single(portrait.InverseLinks);
        Assert.Equal("http://fogid.net/o/depicts", inverse.Predicate);
        Assert.Equal("photo-1", inverse.SourceResourceId);
        Assert.Equal("cass-b", inverse.SourceCassetteId);

        Assert.Equal(SourceRecordId, portrait.Provenance.SourceRecordId);
        Assert.Equal("cass-a", portrait.Provenance.SourceCassetteId);
        Assert.Equal("a.fog", portrait.Provenance.SourceFogPath);
        Assert.Equal(ModifiedAt, portrait.Provenance.ModifiedAt);
        Assert.Equal(2, store.FindCalls);
    }

    [Fact]
    public async Task GetAsync_DoesNotReadTriplesWhenResourceCassetteIsForbidden()
    {
        FakeProjectRdfStore store = new(
            new[]
            {
                new ResourceHead(
                    "person-1",
                    SourceRecordId,
                    "cass-a",
                    "a.fog",
                    ModifiedAt,
                    IsDeleted: false)
            },
            Array.Empty<TripleRow>());
        ProjectResourcePortraitService service = new(store);
        HashSet<string> allowed = new(StringComparer.Ordinal) { "cass-b" };

        ProjectResourcePortrait? portrait = await service.GetAsync("person-1", allowed);

        Assert.Null(portrait);
        Assert.Equal(0, store.FindCalls);
    }

    [Fact]
    public async Task GetAsync_AllowsSyntheticSystemResource()
    {
        FakeProjectRdfStore store = new(
            new[]
            {
                new ResourceHead(
                    "cassetterootcollection",
                    SourceRecordId,
                    "$system",
                    "$synthetic",
                    DateTimeOffset.MinValue,
                    IsDeleted: false)
            },
            new[]
            {
                Triple(
                    "cassetterootcollection",
                    "http://fogid.net/o/name",
                    TripleObjectKind.Literal,
                    "кассеты",
                    "$system")
            });
        ProjectResourcePortraitService service = new(store);

        ProjectResourcePortrait? portrait = await service.GetAsync(
            "cassetterootcollection",
            new HashSet<string>(StringComparer.Ordinal));

        Assert.NotNull(portrait);
        Assert.Equal("кассеты", Assert.Single(portrait.Literals).Value);
    }

    private static TripleRow Triple(
        string subject,
        string predicate,
        TripleObjectKind objectKind,
        string objectValue,
        string cassetteId,
        string? language = null) => new(
            Guid.NewGuid(),
            subject,
            predicate,
            objectKind,
            objectValue,
            language,
            DataType: null,
            SourceRecordId,
            cassetteId,
            cassetteId + ".fog",
            ModifiedAt);

    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

    private sealed class FakeProjectRdfStore : IProjectRdfStore
    {
        private readonly Dictionary<string, ResourceHead> _heads;
        private readonly TripleRow[] _triples;

        public FakeProjectRdfStore(
            IEnumerable<ResourceHead> heads,
            IEnumerable<TripleRow> triples)
        {
            _heads = heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
            _triples = triples.ToArray();
        }

        public int FindCalls { get; private set; }

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
            FindCalls++;
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
