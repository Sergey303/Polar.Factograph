using System.Xml.Linq;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogDirectiveWriterSubstituteTests
{
    [Fact]
    public async Task AppendAsync_WritesSubstituteAndCleansIdentifiers()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        DateTimeOffset now = new(2026, 7, 26, 9, 5, 0, TimeSpan.Zero);
        FileSystemFogDirectiveWriter writer = new(new FixedTimeProvider(now));

        FogDirectiveWriteResult result = await writer.AppendAsync(
            fog.Source,
            new FogDirectiveWriteRequest(
                FogRecordKind.Substitute,
                "existing|",
                "target|1"));

        Assert.Equal("existing", result.ResourceId);
        Assert.Equal("target1", result.SubstituteTargetId);
        Assert.Equal("007", XDocument.Load(fog.Source.FogPath).Root?.Attribute("counter")?.Value);
        List<FogSourceRecord> records = await FogTestRecords.ReadAllAsync(fog.Source);
        FogSourceRecord directive = records.Single(record => record.Kind == FogRecordKind.Substitute);
        Assert.Equal("existing", directive.ResourceId);
        Assert.Equal("target1", directive.SubstituteTargetId);
        Assert.Equal(now.UtcDateTime, directive.ModifiedAt);
    }
}
