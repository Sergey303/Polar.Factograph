using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentUri
{
    public static string Build(
        CassetteDefinition cassette,
        string folderName,
        string documentNumber)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);
        string cassetteName = Uri.EscapeDataString(cassette.Name);
        return $"iiss://{cassetteName}@iis.nsk.su/{folderName}/{documentNumber}";
    }
}
