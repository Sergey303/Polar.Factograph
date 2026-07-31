using System.Globalization;
using System.Text;
using System.Xml;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemFogSourceScanner : IFogSourceScanner
{
    private readonly FogRootMetadataReader _metadataReader = new();

    public async Task<IReadOnlyList<FogSourceDescriptor>> ScanAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        List<FogSourceDescriptor> sources = new();

        foreach (CassetteDefinition cassette in project.Cassettes.Where(cassette => cassette.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string cassettePath = Path.GetFullPath(cassette.Path);
            if (!Directory.Exists(cassettePath))
            {
                throw new DirectoryNotFoundException(
                    $"Cassette directory was not found: {cassettePath}");
            }

            string metadataFogPath = FindCassetteMetadataFog(cassettePath, cassette.Name);
            sources.Add(await DescribeAsync(
                cassette,
                metadataFogPath,
                isCassetteMetadata: true,
                cancellationToken));

            string originalsPath = Path.Combine(cassettePath, "originals");
            if (!Directory.Exists(originalsPath))
            {
                continue;
            }

            IEnumerable<string> additionalFogPaths = Directory
                .EnumerateDirectories(originalsPath)
                .Where(directory => Path.GetFileName(directory).Length == 4)
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                .Where(path => string.Equals(Path.GetExtension(path), ".fog", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (string fogPath in additionalFogPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sources.Add(await DescribeAsync(
                    cassette,
                    fogPath,
                    isCassetteMetadata: false,
                    cancellationToken));
            }
        }

        return sources;
    }

    private static string FindCassetteMetadataFog(string cassettePath, string cassetteName)
    {
        string metadataDirectory = Path.Combine(cassettePath, "meta");
        if (!Directory.Exists(metadataDirectory))
        {
            throw new InvalidDataException(
                $"Cassette '{cassetteName}' has no metadata directory: {metadataDirectory}");
        }

        string expectedName = $"{cassetteName}_current.fog";
        string expectedPath = Path.Combine(metadataDirectory, expectedName);
        if (File.Exists(expectedPath))
        {
            return expectedPath;
        }

        string[] fogFiles = Directory
            .EnumerateFiles(metadataDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(
                Path.GetExtension(path),
                ".fog",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string? caseInsensitiveExpected = fogFiles.FirstOrDefault(path => string.Equals(
            Path.GetFileName(path),
            expectedName,
            StringComparison.OrdinalIgnoreCase));
        if (caseInsensitiveExpected is not null)
        {
            return caseInsensitiveExpected;
        }

        string[] currentCandidates = fogFiles
            .Where(path => Path.GetFileNameWithoutExtension(path)
                .EndsWith("_current", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (currentCandidates.Length == 1)
        {
            return currentCandidates[0];
        }

        if (currentCandidates.Length > 1)
        {
            throw new InvalidDataException(
                $"Cassette '{cassetteName}' has several *_current.fog files in " +
                $"'{metadataDirectory}': {FormatFileNames(currentCandidates)}. " +
                "Rename the actual current file to match the cassette name or leave only one current candidate.");
        }

        if (fogFiles.Length == 1)
        {
            return fogFiles[0];
        }

        string available = fogFiles.Length == 0
            ? "no .fog files"
            : FormatFileNames(fogFiles);
        throw new InvalidDataException(
            $"Cassette '{cassetteName}' has no identifiable current metadata Fog in " +
            $"'{metadataDirectory}'. Expected '{expectedName}'; found {available}.");
    }

    private static string FormatFileNames(IEnumerable<string> paths) =>
        string.Join(", ", paths.Select(path => $"'{Path.GetFileName(path)}'"));

    private async Task<FogSourceDescriptor> DescribeAsync(
        CassetteDefinition cassette,
        string fogPath,
        bool isCassetteMetadata,
        CancellationToken cancellationToken)
    {
        FogRootMetadata metadata = await _metadataReader.ReadAsync(fogPath, cancellationToken);
        FileInfo file = new(fogPath);

        return new FogSourceDescriptor(
            cassette.Id,
            cassette.Name,
            file.FullName,
            metadata.DatabaseId,
            metadata.CassetteUri,
            metadata.Owner,
            metadata.Prefix,
            metadata.Counter,
            cassette.AllowWrite && metadata.Prefix is not null && metadata.Counter is not null,
            isCassetteMetadata,
            file.Length,
            file.LastWriteTimeUtc);
    }
}

internal sealed record FogRootMetadata(
    string? DatabaseId,
    string? CassetteUri,
    string? Owner,
    string? Prefix,
    long? Counter);

internal sealed class FogRootMetadataReader
{
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        Async = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true
    };

    static FogRootMetadataReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<FogRootMetadata> ReadAsync(
        string fogPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fogPath);

        try
        {
            FileStream stream = new(
                fogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using XmlReader reader = XmlReader.Create(stream, ReaderSettings);
            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (!string.Equals(reader.LocalName, "RDF", StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Fog root element must be rdf:RDF: {fogPath}");
                }

                string? counterText = reader.GetAttribute("counter");
                long? counter = null;
                if (!string.IsNullOrWhiteSpace(counterText))
                {
                    if (!long.TryParse(
                            counterText,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out long parsedCounter))
                    {
                        throw new InvalidDataException(
                            $"Fog counter is not an integer in '{fogPath}': {counterText}");
                    }

                    counter = parsedCounter;
                }

                return new FogRootMetadata(
                    reader.GetAttribute("dbid"),
                    reader.GetAttribute("uri"),
                    reader.GetAttribute("owner"),
                    reader.GetAttribute("prefix"),
                    counter);
            }

            throw new InvalidDataException($"Fog file is empty: {fogPath}");
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException(
                $"Fog XML cannot be read: {fogPath}",
                exception);
        }
    }
}
