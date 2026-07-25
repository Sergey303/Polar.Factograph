using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class LegacyFogProjectMaterializerTests
{
    [Fact]
    public async Task Reader_CanonicalizesLegacyRecordsOneAtATime()
    {
        await using TemporaryFog fog = await TemporaryFog.CreateAsync(SyntheticFogXml);
        FileSystemFogRecordReader reader = new();

        List<FogSourceRecord> records = await ReadAllAsync(reader.ReadAsync(fog.Source));

        FogSourceRecord old = records.Single(record =>
            record.Kind == FogRecordKind.Resource &&
            record.ResourceId == "oldid");
        Assert.Equal(FogRecordKind.Resource, old.Kind);
        Assert.Equal("http://fogid.net/o/person", old.Type);
        Assert.Contains(old.Properties, property =>
            property.Predicate == "http://fogid.net/o/friend" &&
            property.Kind == FogPropertyKind.Resource &&
            property.Value == "target1");
        Assert.Contains(old.Properties, property =>
            property.Predicate == "http://fogid.net/o/uri" &&
            property.Value == "iiss://Sample@iis.nsk.su/0001/0001");
        Assert.DoesNotContain(old.Properties, property => property.Predicate.EndsWith("contenttype", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Materializer_PreservesLegacyDeleteSubstituteAndMtRules()
    {
        await using TemporaryFog fog = await TemporaryFog.CreateAsync(SyntheticFogXml);
        FileSystemFogRecordReader reader = new();
        FogProjectRecordSource source = new(reader);
        LegacyFogProjectMaterializer materializer = new();
        IReadOnlyList<FogSourceDescriptor> sources = new[] { fog.Source };

        IAsyncEnumerable<FogSourceRecord> Open(CancellationToken token) => source.ReadAsync(sources, token);

        FogMaterializationPlan plan = await materializer.BuildPlanAsync(Open);
        List<FogCurrentRecord> current = await ReadAllAsync(materializer.ReadCurrentAsync(plan, Open));
        FogMaterializationStatistics summary = await materializer.SummarizeAsync(sources.Count, Open);

        Assert.DoesNotContain(current, record => record.ResourceId == "oldid");
        Assert.DoesNotContain(current, record => record.ResourceId == "deleted");

        FogCurrentRecord duplicate = current.Single(record => record.ResourceId == "dup");
        Assert.Contains(duplicate.Properties, property => property.Value == "latest");

        FogCurrentRecord tied = current.Single(record => record.ResourceId == "tie");
        Assert.Contains(tied.Properties, property => property.Value == "first tie");
        Assert.DoesNotContain(tied.Properties, property => property.Value == "second tie");

        FogCurrentRecord referencing = current.Single(record => record.ResourceId == "source");
        Assert.Contains(referencing.Properties, property =>
            property.Kind == FogPropertyKind.Resource && property.Value == "newid");
        Assert.Contains(referencing.Properties, property =>
            property.Kind == FogPropertyKind.Resource && property.Value == "deleted");

        FogCurrentRecord root = current.Single(record => record.ResourceId == "cassetterootcollection");
        Assert.True(root.IsSynthetic);

        Assert.Equal(10, summary.SourceRecords);
        Assert.Equal(8, summary.ResourceDefinitions);
        Assert.Equal(1, summary.DeleteOperations);
        Assert.Equal(1, summary.SubstituteOperations);
        Assert.Equal(2, summary.DuplicateResourceIds);
        Assert.Equal(1, summary.RedirectedIds);
        Assert.Equal(1, summary.DeletedIds);
        Assert.Equal(4, summary.CurrentSourceResources);
        Assert.Equal(1, summary.SyntheticResources);
    }

    [Fact]
    public async Task Materializer_RejectsCyclicSubstitutions()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
              <substitute old-id="a" new-id="b" />
              <substitute old-id="b" new-id="a" />
            </rdf:RDF>
            """;

        await using TemporaryFog fog = await TemporaryFog.CreateAsync(xml);
        FileSystemFogRecordReader reader = new();
        FogProjectRecordSource source = new(reader);
        LegacyFogProjectMaterializer materializer = new();
        IReadOnlyList<FogSourceDescriptor> sources = new[] { fog.Source };

        IAsyncEnumerable<FogSourceRecord> Open(CancellationToken token) => source.ReadAsync(sources, token);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => materializer.BuildPlanAsync(Open));

        Assert.Contains("Cyclic Fog substitute chain", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Materializer_ProcessesRealSypCassette()
    {
        string configurationPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "syp.project.json");

        ProjectConfigurationLoader loader = new();
        ProjectDefinition project = await loader.LoadAsync(configurationPath);
        FileSystemFogSourceScanner scanner = new();
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(project);
        FileSystemFogRecordReader reader = new();
        FogProjectRecordSource source = new(reader);
        LegacyFogProjectMaterializer materializer = new();

        IAsyncEnumerable<FogSourceRecord> Open(CancellationToken token) => source.ReadAsync(sources, token);

        FogMaterializationStatistics summary = await materializer.SummarizeAsync(sources.Count, Open);

        Assert.Equal(1, summary.SourceFiles);
        Assert.True(summary.SourceRecords > 1_000);
        Assert.True(summary.ResourceDefinitions > 1_000);
        Assert.True(summary.CurrentSourceResources > 1_000);
        Assert.True(summary.CurrentProperties > summary.CurrentSourceResources);
    }

    private static async Task<List<T>> ReadAllAsync<T>(IAsyncEnumerable<T> source)
    {
        List<T> result = new();
        await foreach (T item in source)
        {
            result.Add(item);
        }

        return result;
    }

    private const string SyntheticFogXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <person rdf:about="old|id" mT="2020-01-01T00:00:00">
            <name xml:lang="ru">Old</name>
            <friend rdf:resource="target|1" />
            <iisstore uri="iiss://Sample@iis.nsk.su/0001/0001" contenttype="image/jpeg" />
          </person>
          <person rdf:about="dup" mT="2020-01-01T00:00:00"><name>old</name></person>
          <person rdf:about="dup" mT="2021-01-01T00:00:00"><name>latest</name></person>
          <person rdf:about="tie" mT="2022-01-01T00:00:00"><name>first tie</name></person>
          <person rdf:about="tie" mT="2022-01-01T00:00:00"><name>second tie</name></person>
          <substitute old-id="oldid" new-id="newid" />
          <delete id="deleted" />
          <person rdf:about="source">
            <friend rdf:resource="oldid" />
            <friend rdf:resource="deleted" />
          </person>
          <person rdf:about="newid"><name>new</name></person>
          <person rdf:about="deleted"><name>gone</name></person>
        </rdf:RDF>
        """;

    private sealed class TemporaryFog : IAsyncDisposable
    {
        private TemporaryFog(string directory, FogSourceDescriptor source)
        {
            Directory = directory;
            Source = source;
        }

        public string Directory { get; }
        public FogSourceDescriptor Source { get; }

        public static async Task<TemporaryFog> CreateAsync(string xml)
        {
            string directory = Path.Combine(Path.GetTempPath(), "polar-factograph-tests", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "test.fog");
            await File.WriteAllTextAsync(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            FileInfo file = new(path);

            FogSourceDescriptor source = new(
                "test-cassette",
                "TestCassette",
                file.FullName,
                "test",
                "iiss://TestCassette@iis.nsk.su",
                "tester",
                null,
                null,
                Writable: false,
                IsCassetteMetadata: true,
                file.Length,
                file.LastWriteTimeUtc);

            return new TemporaryFog(directory, source);
        }

        public ValueTask DisposeAsync()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
