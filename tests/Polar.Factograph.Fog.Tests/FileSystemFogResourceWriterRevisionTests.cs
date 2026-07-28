using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogResourceWriterRevisionTests
{
    [Fact]
    public async Task AppendAsync_AdvancesTimestampWhenExistingRevisionHasSameTime()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        DateTimeOffset existingTime = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FileSystemFogResourceWriter writer = new(new FixedTimeProvider(existingTime));
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Updated")],
            ResourceId: "existing");

        FogResourceWriteResult result = await writer.AppendAsync(fog.Source, request);

        Assert.Equal(existingTime.UtcDateTime.AddSeconds(1), result.ModifiedAtUtc);
        FileSystemFogRecordReader reader = new();
        FogProjectRecordSource source = new(reader);
        IReadOnlyList<FogSourceDescriptor> sources = [fog.Source];
        IAsyncEnumerable<FogSourceRecord> Open(CancellationToken token) =>
            source.ReadAsync(sources, token);
        LegacyFogProjectMaterializer materializer = new();
        FogMaterializationPlan plan = await materializer.BuildPlanAsync(Open);
        List<FogCurrentRecord> current = await FogTestRecords.ReadAllAsync(
            materializer.ReadCurrentAsync(plan, Open));

        FogCurrentRecord resource = current.Single(record => record.ResourceId == "existing");
        Assert.Contains(resource.Properties, property => property.Value == "Updated");
        Assert.DoesNotContain(resource.Properties, property => property.Value == "Existing");
    }

    [Fact]
    public async Task AppendAsync_AcceptsRealClockFractionalSeconds()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        DateTimeOffset now = new(2026, 7, 28, 12, 34, 56, 789, TimeSpan.Zero);
        FileSystemFogResourceWriter writer = new(new FixedTimeProvider(now));
        FogResourceWriteRequest request = new(
            "photo-doc",
            [
                new FogProperty("uri", FogPropertyKind.Literal, "iiss://Cassette@iis/0001/0002"),
                new FogProperty("name", FogPropertyKind.Literal, "Описание фотографии")
            ]);

        FogResourceWriteResult result = await writer.AppendAsync(fog.Source, request);

        Assert.Equal(
            new DateTime(2026, 7, 28, 12, 34, 56, DateTimeKind.Utc),
            result.ModifiedAtUtc);
        FileSystemFogRecordReader reader = new();
        List<FogSourceRecord> records = [];
        await foreach (FogSourceRecord record in reader.ReadAsync(fog.Source))
        {
            records.Add(record);
        }

        Assert.Contains(records, record =>
            record.Kind == FogRecordKind.Resource &&
            record.ResourceId == result.ResourceId &&
            record.ModifiedAt == result.ModifiedAtUtc);
    }
}
