namespace Polar.Factograph.Api.Documents;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "Documents";
    public const long DefaultMaxUploadBytes = 1_073_741_824;

    public long MaxUploadBytes { get; init; } = DefaultMaxUploadBytes;
}
