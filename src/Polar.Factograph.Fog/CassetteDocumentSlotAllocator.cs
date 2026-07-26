using System.Globalization;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal sealed record CassetteDocumentSlot(
    string FolderName,
    string DocumentNumber,
    string Path);

internal static class CassetteDocumentSlotAllocator
{
    private const int MaxNumber = 9_999;

    public static CassetteDocumentSlot Allocate(
        CassetteDefinition cassette,
        string extension)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        string originals = Path.Combine(Path.GetFullPath(cassette.Path), "originals");
        Directory.CreateDirectory(originals);
        (int folder, int document) = FindMaximum(originals);
        (folder, document) = Next(folder, document);
        string folderName = folder.ToString("D4", CultureInfo.InvariantCulture);
        string documentNumber = document.ToString("D4", CultureInfo.InvariantCulture);
        string directory = Path.Combine(originals, folderName);
        Directory.CreateDirectory(directory);
        return new CassetteDocumentSlot(
            folderName,
            documentNumber,
            Path.Combine(directory, documentNumber + extension));
    }

    private static (int Folder, int Document) FindMaximum(string originals)
    {
        (int Folder, int Document) maximum = (0, 0);
        foreach (string directory in Directory.EnumerateDirectories(originals))
        {
            if (!TryNumber(Path.GetFileName(directory), out int folder))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (TryNumber(Path.GetFileNameWithoutExtension(file), out int document) &&
                    (folder, document).CompareTo(maximum) > 0)
                {
                    maximum = (folder, document);
                }
            }
        }

        return maximum;
    }

    private static (int Folder, int Document) Next(int folder, int document)
    {
        if (folder == 0)
        {
            return (1, 1);
        }

        if (document < MaxNumber)
        {
            return (folder, document + 1);
        }

        return folder < MaxNumber
            ? (folder + 1, 1)
            : throw new IOException("Cassette document number space is exhausted.");
    }

    private static bool TryNumber(string value, out int number) =>
        value.Length == 4 &&
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) &&
        number is > 0 and <= MaxNumber;
}
