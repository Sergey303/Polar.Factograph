using System.Globalization;
using System.Text.Json;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectIndexVerificationReport(
    int SchemaVersion,
    string ProjectId,
    string GenerationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int SourceFiles,
    int ExpectedResources,
    int StoredResources,
    int MissingResources,
    int ExtraResources,
    IReadOnlyList<string> MissingResourceExamples,
    IReadOnlyList<string> ExtraResourceExamples,
    int ExpectedTriples,
    int StoredTriples,
    int MissingTriples,
    int ExtraTriples,
    IReadOnlyList<string> MissingTripleExamples,
    IReadOnlyList<string> ExtraTripleExamples,
    int DifferenceSampleLimit,
    bool DifferenceSamplesTruncated,
    bool IsMatch,
    string ReportPath);

/// <summary>
/// Performs an explicit, memory-backed comparison between the current Fog materialization
/// and the active Polar.DB generation, then keeps the result as a JSON audit file.
/// </summary>
public sealed class ProjectIndexVerificationCoordinator(
    IFogSourceScanner sourceScanner,
    FogProjectRecordSource recordSource,
    LegacyFogProjectMaterializer materializer,
    ProjectOperationGate operationGate,
    ILogger<ProjectIndexVerificationCoordinator> logger)
{
    private const int DifferenceSampleLimit = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly CurrentRecordTripleProjector _projector = new();

    public async Task<ProjectIndexVerificationReport> VerifyAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;

        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            project.Index.Path,
            cancellationToken);

        IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
            project,
            cancellationToken);
        if (sources.Count == 0)
        {
            throw new InvalidOperationException(
                "No Fog sources were found for the enabled project cassettes.");
        }

        FogRecordStreamFactory openRecords = token => recordSource.ReadAsync(sources, token);
        FogMaterializationPlan plan = await materializer.BuildPlanAsync(
            openRecords,
            cancellationToken);

        HashSet<string> expectedResourceIds = new(StringComparer.Ordinal);
        HashSet<Guid> expectedTripleIds = [];
        await foreach (FogCurrentRecord current in materializer
                           .ReadCurrentAsync(plan, openRecords, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            ProjectedResource projected = _projector.Project(current);
            expectedResourceIds.Add(projected.Head.ResourceId);
            foreach (TripleRow triple in projected.Triples)
            {
                expectedTripleIds.Add(triple.TripleId);
            }
        }

        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            project.Index.Path);
        string generationId = Path.GetFileName(
            store.GenerationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        HashSet<string> storedResourceIds = store
            .ReadAllResourceHeads()
            .Select(row => row.ResourceId)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<Guid> storedTripleIds = store
            .ReadAllTriples()
            .Select(row => row.TripleId)
            .ToHashSet();

        string[] missingResources = expectedResourceIds
            .Except(storedResourceIds, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] extraResources = storedResourceIds
            .Except(expectedResourceIds, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Guid[] missingTriples = expectedTripleIds
            .Except(storedTripleIds)
            .Order()
            .ToArray();
        Guid[] extraTriples = storedTripleIds
            .Except(expectedTripleIds)
            .Order()
            .ToArray();

        bool isMatch = missingResources.Length == 0 &&
            extraResources.Length == 0 &&
            missingTriples.Length == 0 &&
            extraTriples.Length == 0;
        bool samplesTruncated = missingResources.Length > DifferenceSampleLimit ||
            extraResources.Length > DifferenceSampleLimit ||
            missingTriples.Length > DifferenceSampleLimit ||
            extraTriples.Length > DifferenceSampleLimit;

        DateTimeOffset completedAtUtc = DateTimeOffset.UtcNow;
        string reportDirectory = Path.Combine(
            Path.GetFullPath(project.Index.Path),
            "verification");
        Directory.CreateDirectory(reportDirectory);
        string reportName = string.Create(
            CultureInfo.InvariantCulture,
            $"index-verification-{completedAtUtc:yyyyMMddTHHmmssfffZ}-{generationId}.json");
        string reportPath = Path.Combine(reportDirectory, reportName);

        ProjectIndexVerificationReport report = new(
            SchemaVersion: 1,
            project.ProjectId,
            generationId,
            startedAtUtc,
            completedAtUtc,
            sources.Count,
            expectedResourceIds.Count,
            storedResourceIds.Count,
            missingResources.Length,
            extraResources.Length,
            missingResources.Take(DifferenceSampleLimit).ToArray(),
            extraResources.Take(DifferenceSampleLimit).ToArray(),
            expectedTripleIds.Count,
            storedTripleIds.Count,
            missingTriples.Length,
            extraTriples.Length,
            missingTriples
                .Take(DifferenceSampleLimit)
                .Select(value => value.ToString("D", CultureInfo.InvariantCulture))
                .ToArray(),
            extraTriples
                .Take(DifferenceSampleLimit)
                .Select(value => value.ToString("D", CultureInfo.InvariantCulture))
                .ToArray(),
            DifferenceSampleLimit,
            samplesTruncated,
            isMatch,
            reportPath);

        await WriteReportAsync(report, reportPath, cancellationToken);
        logger.LogInformation(
            "Verified project index generation {GenerationId}. Match: {IsMatch}; " +
            "resources expected/stored/missing/extra: {ExpectedResources}/{StoredResources}/{MissingResources}/{ExtraResources}; " +
            "triples expected/stored/missing/extra: {ExpectedTriples}/{StoredTriples}/{MissingTriples}/{ExtraTriples}; " +
            "report: {ReportPath}.",
            report.GenerationId,
            report.IsMatch,
            report.ExpectedResources,
            report.StoredResources,
            report.MissingResources,
            report.ExtraResources,
            report.ExpectedTriples,
            report.StoredTriples,
            report.MissingTriples,
            report.ExtraTriples,
            report.ReportPath);
        return report;
    }

    private static async Task WriteReportAsync(
        ProjectIndexVerificationReport report,
        string reportPath,
        CancellationToken cancellationToken)
    {
        string temporaryPath = reportPath + ".tmp";
        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    report,
                    JsonOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, reportPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
