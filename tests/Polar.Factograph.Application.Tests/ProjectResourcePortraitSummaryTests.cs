using System.Runtime.CompilerServices;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectResourcePortraitSummaryTests
{
    [Fact]
    public async Task GetSummaryAsync_DoesNotReadInverseLinks()
    {
        SummaryStore store = new();
        ProjectResourcePortraitService service = new(store);

        ProjectResourcePortrait? portrait = await service.GetSummaryAsync(
            "city-1",
            new HashSet<string>(["cass"], StringComparer.Ordinal));

        Assert.NotNull(portrait);
        Assert.Equal("city-1", portrait.ResourceId);
        Assert.Equal("http://fogid.net/o/city", portrait.Type);
        Assert.Equal("Новосибирск", Assert.Single(portrait.Literals).Value);
        Assert.Empty(portrait.InverseLinks);
        Assert.False(store.InverseQueryAttempted);
    }

    private sealed class SummaryStore : IProjectRdfStore
    {
        private static readonly DateTimeOffset ModifiedAt =
            new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        public bool InverseQueryAttempted { get; private set; }

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ResourceHead?>(new ResourceHead(
                resourceId,
                Guid.NewGuid(),
                "cass",
                "source.fog",
                ModifiedAt,
                IsDeleted: false));

        public async IAsyncEnumerable<TripleRow> FindAsync(
            TriplePattern pattern,
            IReadOnlySet<string> allowedCassetteIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (pattern.ObjectValue is not null)
            {
                InverseQueryAttempted = true;
                throw new InvalidOperationException("Summary reads must not query inverse links.");
            }

            if (!string.Equals(pattern.Subject, "city-1", StringComparison.Ordinal))
            {
                yield break;
            }

            yield return Triple(
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                TripleObjectKind.Iri,
                "http://fogid.net/o/city",
                language: null);
            yield return Triple(
                "http://fogid.net/o/name",
                TripleObjectKind.Literal,
                "Новосибирск",
                "ru");
        }

        private static TripleRow Triple(
            string predicate,
            TripleObjectKind objectKind,
            string objectValue,
            string? language) => new(
                Guid.NewGuid(),
                "city-1",
                predicate,
                objectKind,
                objectValue,
                language,
                DataType: null,
                Guid.NewGuid(),
                "cass",
                "source.fog",
                ModifiedAt);
    }
}
