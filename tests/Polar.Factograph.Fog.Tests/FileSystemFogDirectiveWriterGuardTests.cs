using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogDirectiveWriterGuardTests
{
    [Fact]
    public async Task AppendAsync_RejectsSelfSubstituteWithoutChangingFile()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        string before = await File.ReadAllTextAsync(fog.Source.FogPath);
        FogDirectiveWriteRequest request = new(
            FogRecordKind.Substitute,
            "same|id",
            "sameid");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new FileSystemFogDirectiveWriter().AppendAsync(fog.Source, request));

        Assert.Equal(before, await File.ReadAllTextAsync(fog.Source.FogPath));
        Assert.Empty(Directory.EnumerateFiles(fog.Directory, "*.tmp"));
    }
}
