using System.Globalization;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentNumber
{
    private const int Maximum = 9_999;

    public static (int Folder, int Document) Next(int folder, int document)
    {
        if (folder == 0)
        {
            return (1, 1);
        }

        if (document < Maximum)
        {
            return (folder, document + 1);
        }

        return folder < Maximum
            ? (folder + 1, 1)
            : throw new IOException("Cassette document number space is exhausted.");
    }

    public static bool TryParse(string value, out int number)
    {
        number = 0;
        return value.Length == 4 &&
               int.TryParse(
                   value,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out number) &&
               number is > 0 and <= Maximum;
    }
}
