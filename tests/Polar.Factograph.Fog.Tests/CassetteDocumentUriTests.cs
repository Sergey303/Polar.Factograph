namespace Polar.Factograph.Fog.Tests;

public sealed class CassetteDocumentUriTests
{
    [Theory]
    [InlineData("iiss://Syp2014@iis.nsk.su/0001/0002")]
    [InlineData("iiss://PA_folders24-59@iis.nsk.su/0003/0892")]
    public void IsDocumentUri_AcceptsPhysicalDocumentCoordinates(string value)
    {
        Assert.True(CassetteDocumentPathResolver.IsDocumentUri(value));
    }

    [Theory]
    [InlineData("iiss://Syp2014@iis.nsk.su/meta")]
    [InlineData("iiss://Syp2014@iis.nsk.su/")]
    [InlineData("https://Syp2014@iis.nsk.su/0001/0002")]
    [InlineData("iiss://Syp2014@iis.nsk.su/001/0002")]
    [InlineData("iiss://Syp2014@iis.nsk.su/0001/002")]
    public void IsDocumentUri_RejectsMetadataAndMalformedUris(string value)
    {
        Assert.False(CassetteDocumentPathResolver.IsDocumentUri(value));
    }
}
