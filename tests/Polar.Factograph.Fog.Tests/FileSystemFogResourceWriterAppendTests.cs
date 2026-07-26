using System.Xml.Linq;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogResourceWriterAppendTests
{
    [Fact]
    public async Task AppendAsync_GeneratesIdUpdatesCounterAndPreservesCompatibleProperties()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        DateTimeOffset now = new(2026, 7, 26, 6, 15, 30, TimeSpan.Zero);
        FileSystemFogResourceWriter writer = new(new FixedTimeProvider(now));
        FogResourceWriteRequest request = new(
            LegacyFogVocabulary.Namespace + "person",
            [
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "name",
                    FogPropertyKind.Literal,
                    "Alice",
                    Language: "ru"),
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "alias",
                    FogPropertyKind.Literal,
                    string.Empty),
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "friend",
                    FogPropertyKind.Resource,
                    "target|1"),
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "score",
                    FogPropertyKind.Literal,
                    "42",
                    DataType: "http://www.w3.org/2001/XMLSchema#integer")
            ]);

        FogResourceWriteResult result = await writer.AppendAsync(fog.Source, request);

        Assert.Equal("p7", result.ResourceId);
        Assert.Equal(8, result.NextCounter);
        Assert.Equal(now.UtcDateTime, result.ModifiedAtUtc);
        Assert.Equal("8", XDocument.Load(fog.Source.FogPath).Root?.Attribute("counter")?.Value);

        List<FogSourceRecord> records = await FogTestRecords.ReadAllAsync(fog.Source);
        Assert.Contains(records, record => record.ResourceId == "existing");
        FogSourceRecord written = records.Single(record => record.ResourceId == "p7");
        Assert.Equal(LegacyFogVocabulary.Namespace + "person", written.Type);
        Assert.Equal(now.UtcDateTime, written.ModifiedAt);
        Assert.Contains(written.Properties, property =>
            property.Predicate.EndsWith("name", StringComparison.Ordinal) &&
            property.Value == "Alice" && property.Language == "ru");
        Assert.DoesNotContain(written.Properties, property =>
            property.Predicate.EndsWith("alias", StringComparison.Ordinal));
        Assert.Contains(written.Properties, property =>
            property.Kind == FogPropertyKind.Resource && property.Value == "target1");
        Assert.Contains(written.Properties, property =>
            property.Value == "42" &&
            property.DataType == "http://www.w3.org/2001/XMLSchema#integer");
    }
}
