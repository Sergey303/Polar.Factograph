using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentWriteResultFactory
{
    public static CassetteDocumentWriteResult Create(
        CassetteDefinition cassette,
        string documentUri,
        string folderName,
        string documentNumber,
        string path,
        CassetteDocumentCopyResult copy,
        bool replaced) => new(
            cassette.Id,
            cassette.Name,
            documentUri,
            folderName,
            documentNumber,
            Path.GetFileName(path),
            copy.Length,
            copy.Sha256,
            replaced);
}
