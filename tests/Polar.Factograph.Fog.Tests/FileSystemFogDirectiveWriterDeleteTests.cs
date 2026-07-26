using System.Xml.Linq;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogDirectiveWriterDeleteTests
{
    [Fact]
    public async Task AppendAsync_WritesDeleteWithoutChangingCounter()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        DateTimeOffset now = new(2026, 7, 26, 9, 0, 0, TimeSpan.Zero);
        FileSystemFogDirectiveWriter writer = new(new FixedTimeProvider(now));

        FogDirectiveWriteResult result = await writer.AppendAsync(
            fog.Source,
            new FogDirectiveWriteRequest(FogRecordKind.Delete, "existing|"));

        Assert.Equal(FogRecordKind.Delete, result.Kind);
        Assert.Equal("existing", result.ResourceId);
        Assert.Equal("007", XDocument.Load(fog.Source.FogPath).Root?.Attribute("counter")?.Value);
        List<FogSourceRecord> records = await FogTestRecords.ReadAllAsync(fog.Source);
        FogSourceRecord directive = records.Single(record => record.Kind == FogRecordKind.Delete);
        Assert.Equal("existing", directive.ResourceId);
        Assert.Equal(now.UtcDateTime, directive.ModifiedAt);
    }
}
