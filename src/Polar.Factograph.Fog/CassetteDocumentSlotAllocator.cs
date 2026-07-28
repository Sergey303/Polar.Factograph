using System.Globalization;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentSlotAllocator
{
    private static readonly string[][] DocumentTrees =
    [
        ["originals"],
        ["documents", "small"],
        ["documents", "medium"],
        ["documents", "normal"]
    ];

    public static CassetteDocumentSlot Allocate(
        CassetteDefinition cassette,
        string extension)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        string cassetteRoot = Path.GetFullPath(cassette.Path);
        string originals = Path.Combine(cassetteRoot, "originals");
        Directory.CreateDirectory(originals);
        (int folder, int document) = FindMaximum(cassetteRoot);
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

    private static (int Folder, int Document) FindMaximum(string cassetteRoot)
    {
        (int Folder, int Document) maximum = (0, 0);
        foreach (string[] relativeParts in DocumentTrees)
        {
            string root = relativeParts.Aggregate(cassetteRoot, Path.Combine);
            maximum = Max(maximum, FindMaximumInTree(root));
        }
        return maximum;
    }

    private static (int Folder, int Document) FindMaximumInTree(string root)
    {
        (int Folder, int Document) maximum = (0, 0);
        if (!Directory.Exists(root))
        {
            return maximum;
        }

        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            if (!CassetteDocumentNumber.TryParse(
                    Path.GetFileName(directory),
                    out int folder))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (TryParseDocumentNumber(file, out int document))
                {
                    maximum = Max(maximum, (folder, document));
                }
            }
        }

        return maximum;
    }

    private static (int Folder, int Document) Max(
        (int Folder, int Document) left,
        (int Folder, int Document) right) => right.CompareTo(left) > 0 ? right : left;

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
