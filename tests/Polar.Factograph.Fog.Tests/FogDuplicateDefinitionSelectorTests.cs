using System.Runtime.CompilerServices;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FogDuplicateDefinitionSelectorTests
{
    [Fact]
    public async Task Materializer_prefers_later_source_when_modified_dates_are_equal()
    {
        FogSourceRecord first = Resource(
            "first.fog",
            "first",
            DateTime.MinValue,
            "Ранняя копия");
        FogSourceRecord second = Resource(
            "second.fog",
            "second",
            DateTime.MinValue,
            "Полная копия из более поздней кассеты");

        FogCurrentRecord current = await MaterializeSingleAsync([first, second]);

        Assert.Equal("second", current.SourceCassetteId);
        Assert.Contains(current.Properties, property =>
            property.Value == "Полная копия из более поздней кассеты");
    }

    [Fact]
    public async Task Materializer_keeps_newer_modified_date_even_when_source_is_earlier()
    {
        FogSourceRecord newer = Resource(
            "newer.fog",
            "newer",
            new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            "Новая версия");
        FogSourceRecord laterButOlder = Resource(
            "older.fog",
            "older",
            new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            "Старая версия");

        FogCurrentRecord current = await MaterializeSingleAsync([newer, laterButOlder]);

        Assert.Equal("newer", current.SourceCassetteId);
        Assert.Contains(current.Properties, property => property.Value == "Новая версия");
    }

    private static async Task<FogCurrentRecord> MaterializeSingleAsync(
        IReadOnlyList<FogSourceRecord> records)
    {
        LegacyFogProjectMaterializer materializer = new();
        IAsyncEnumerable<FogSourceRecord> Open(CancellationToken token) =>
            ReadAsync(records, token);

        FogMaterializationPlan plan = await materializer.BuildPlanAsync(Open);
        List<FogCurrentRecord> current = [];
        await foreach (FogCurrentRecord record in materializer.ReadCurrentAsync(plan, Open))
        {
            if (!record.IsSynthetic)
            {
                current.Add(record);
            }
        }

        return Assert.Single(current);
    }

    private static async IAsyncEnumerable<FogSourceRecord> ReadAsync(
        IReadOnlyList<FogSourceRecord> records,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (FogSourceRecord record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return record;
        }
    }

    private static FogSourceRecord Resource(
        string fogPath,
        string cassetteId,
        DateTime modifiedAt,
        string name) => new(
        new FogRecordKey(fogPath, 0),
        cassetteId,
        cassetteId,
        FogRecordKind.Resource,
        "shared-resource",
        "http://fogid.net/o/person",
        SubstituteTargetId: null,
        modifiedAt,
        modifiedAt == DateTime.MinValue ? null : modifiedAt.ToString("O"),
        [
            new FogProperty(
                "http://fogid.net/o/name",
                FogPropertyKind.Literal,
                name,
                "ru")
        ]);
}
