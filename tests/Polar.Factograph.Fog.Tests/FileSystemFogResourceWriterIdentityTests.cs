using System.Xml.Linq;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogResourceWriterIdentityTests
{
    [Fact]
    public async Task AppendAsync_UsesExplicitIdWithoutChangingCounter()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        FileSystemFogResourceWriter writer = new(new FixedTimeProvider(
            new DateTimeOffset(2026, 7, 26, 7, 0, 0, TimeSpan.Zero)));
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Manual")],
            ResourceId: "manual|id");

        FogResourceWriteResult result = await writer.AppendAsync(fog.Source, request);

        Assert.Equal("manualid", result.ResourceId);
        Assert.Equal(7, result.NextCounter);
        Assert.Equal(
            "007",
            XDocument.Load(fog.Source.FogPath).Root?.Attribute("counter")?.Value);
        List<FogSourceRecord> records = await FogTestRecords.ReadAllAsync(fog.Source);
        Assert.Contains(records, record => record.ResourceId == "manualid");
    }
}
