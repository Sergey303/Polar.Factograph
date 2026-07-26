using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassettePreviewRequestValidator
{
    public static void Validate(
        CassetteDefinition cassette,
        CassettePreviewRequest request)
    {
        if (!Guid.TryParseExact(request.RequestId, "N", out _))
        {
            throw new InvalidDataException("Preview request id is invalid.");
        }

        if (!string.Equals(cassette.Id, request.CassetteId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Preview request belongs to another cassette.");
        }

        ValidateSlot(request.FolderName, "folder");
        ValidateSlot(request.DocumentNumber, "document number");
        if (string.IsNullOrWhiteSpace(request.DocumentUri) ||
            string.IsNullOrWhiteSpace(request.OriginalFileName) ||
            !string.Equals(
                request.OriginalFileName,
                Path.GetFileName(request.OriginalFileName),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("Preview request original file name is invalid.");
        }

        if (request.OriginalLength <= 0 ||
            request.OriginalSha256.Length != 64 ||
            request.OriginalSha256.Any(character => !Uri.IsHexDigit(character)) ||
            request.Attempt < 0)
        {
            throw new InvalidDataException("Preview request file metadata is invalid.");
        }
    }

    private static void ValidateSlot(string value, string description)
    {
        if (value.Length != 4 ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            value.Contains(Path.DirectorySeparatorChar) ||
            value.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidDataException($"Preview request {description} is invalid.");
        }
    }
}