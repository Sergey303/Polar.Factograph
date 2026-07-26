namespace Polar.Factograph.Api.Documents;

internal static class DocumentUploadRules
{
    public static long RequireLength(long? contentLength, long maxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (contentLength == 0)
        {
            throw new InvalidDataException("Document content must not be empty.");
        }

        if (contentLength > maxBytes)
        {
            throw new InvalidDataException(
                $"Document exceeds the {maxBytes}-byte upload limit.");
        }

        return maxBytes;
    }
}
