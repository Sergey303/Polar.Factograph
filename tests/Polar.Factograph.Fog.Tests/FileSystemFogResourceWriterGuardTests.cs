using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogResourceWriterGuardTests
{
    [Fact]
    public async Task AppendAsync_RejectsNonWritableSourceWithoutChangingFile()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        string before = await File.ReadAllTextAsync(fog.Source.FogPath);
        FogSourceDescriptor readOnly = fog.Source with { Writable = false };
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Blocked")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FileSystemFogResourceWriter().AppendAsync(readOnly, request));

        Assert.Equal(before, await File.ReadAllTextAsync(fog.Source.FogPath));
    }

    [Fact]
    public async Task AppendAsync_RejectsInvalidTypeWithoutReplacingOriginal()
    {
        await using WritableFogFixture fog = await WritableFogFixture.CreateAsync();
        string before = await File.ReadAllTextAsync(fog.Source.FogPath);
        FogResourceWriteRequest request = new(
            "not/a/name",
            [new FogProperty("name", FogPropertyKind.Literal, "Blocked")]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new FileSystemFogResourceWriter().AppendAsync(fog.Source, request));

        Assert.Equal(before, await File.ReadAllTextAsync(fog.Source.FogPath));
        Assert.Empty(Directory.EnumerateFiles(fog.Directory, "*.tmp"));
    }
}
