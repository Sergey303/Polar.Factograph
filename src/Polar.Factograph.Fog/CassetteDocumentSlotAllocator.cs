using System.Globalization;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentSlotAllocator
{
    public static CassetteDocumentSlot Allocate(
        CassetteDefinition cassette,
        string extension)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        string originals = Path.Combine(Path.GetFullPath(cassette.Path), "originals");
        Directory.CreateDirectory(originals);
        (int folder, int document) = FindMaximum(originals);
        (folder, document) = CassetteDocumentNumber.Next(folder, document);
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
            if (!CassetteDocumentNumber.TryParse(
                    Path.GetFileName(directory),
                    out int folder))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (TryParseDocumentNumber(file, out int document) &&
                    (folder, document).CompareTo(maximum) > 0)
                {
                    maximum = (folder, document);
                }
            }
        }

        return maximum;
    }

    private static bool TryParseDocumentNumber(string path, out int document)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (CassetteDocumentNumber.TryParse(name, out document))
        {
            return true;
        }

        return name.Length > 5 &&
               name[4] == '-' &&
               CassetteDocumentNumber.TryParse(name[..4], out document);
    }
}
