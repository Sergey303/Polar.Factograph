using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FogWritableSourceSelectorTests
{
    [Fact]
    public void Select_PrefersCassetteMetadataFog()
    {
        FogSourceDescriptor additional = Source("z.fog", isMetadata: false, writable: true);
        FogSourceDescriptor metadata = Source("meta.fog", isMetadata: true, writable: true);

        FogSourceDescriptor result = FogWritableSourceSelector.Select(
            [additional, metadata],
            "cassette");

        Assert.Same(metadata, result);
    }

    [Fact]
    public void Select_RejectsCassetteWithoutWritableFog()
    {
        FogSourceDescriptor readOnly = Source("meta.fog", isMetadata: true, writable: false);

        Assert.Throws<InvalidOperationException>(() =>
            FogWritableSourceSelector.Select([readOnly], "cassette"));
    }

    private static FogSourceDescriptor Source(
        string path,
        bool isMetadata,
        bool writable) => new(
        "cassette",
        "Cassette",
        path,
        "database",
        "iiss://Cassette@iis.nsk.su",
        "owner",
        writable ? "p" : null,
        writable ? 1 : null,
        writable,
        isMetadata,
        Length: 1,
        LastWriteTimeUtc: DateTime.UtcNow);
}
