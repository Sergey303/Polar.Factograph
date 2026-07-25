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
            throw new DirectoryNotFoundException(
                $"Cassette metadata directory was not found: {metadataDirectory}");
        }

        string expectedName = $"{cassetteName}_current.fog";
        string expectedPath = Path.Combine(metadataDirectory, expectedName);
        if (File.Exists(expectedPath))
        {
            return expectedPath;
        }

        string? caseInsensitiveMatch = Directory
            .EnumerateFiles(metadataDirectory, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => string.Equals(
                Path.GetFileName(path),
                expectedName,
                StringComparison.OrdinalIgnoreCase));

        return caseInsensitiveMatch
            ?? throw new FileNotFoundException(
                $"Current cassette Fog was not found: {expectedPath}",
                expectedPath);
    }

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
