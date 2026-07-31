using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record EditorFogValidationStatistics(
    int ConfiguredEditors,
    int RegisteredEditors,
    int UnregisteredEditors,
    int EditorsWithFog,
    int ValidEditorFogs,
    int InvalidEditorFogs,
    int UnassignedWritableFogs);

public sealed record ProjectFullRefreshResult(
    Guid GenerationId,
    int EnabledCassettes,
    int ScannedCassettes,
    int SourceFiles,
    EditorFogValidationStatistics Editors,
    ProjectIndexBuildStatistics Statistics);

public sealed class ProjectFullRefreshCoordinator(
    LocalAuthenticationService authentication,
    IdentityJsonStore identityStore,
    LocalAuthenticationOptions authenticationOptions,
    IFogSourceScanner sourceScanner,
    ProjectIndexCoordinator indexCoordinator,
    ILogger<ProjectFullRefreshCoordinator> logger)
{
    public async Task<ProjectFullRefreshResult> RefreshAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        int enabledCassettes = project.Cassettes.Count(cassette => cassette.Enabled);
        logger.LogInformation(
            "Starting full project refresh for {ProjectId}: {CassetteCount} enabled cassettes.",
            project.ProjectId,
            enabledCassettes);

        try
        {
            await authentication.ProvisionConfiguredEditorsAsync(cancellationToken);

            IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
                project,
                cancellationToken);
            if (sources.Count == 0)
            {
                throw new InvalidOperationException(
                    "No Fog sources were found for the enabled project cassettes.");
            }

            EditorFogValidationStatistics editors = EditorFogAssignmentValidator.Validate(
                project,
                sources,
                identityStore.Current,
                authenticationOptions);
            int scannedCassettes = sources
                .Select(source => source.CassetteId)
                .Distinct(StringComparer.Ordinal)
                .Count();

            logger.LogInformation(
                "Validated {SourceFileCount} Fog sources from {ScannedCassetteCount} cassettes. " +
                "Configured editors: {ConfiguredEditorCount}; registered: {RegisteredEditorCount}; " +
                "unregistered: {UnregisteredEditorCount}; valid editor Fogs: {ValidEditorFogCount}; " +
                "unassigned writable Fogs: {UnassignedWritableFogCount}.",
                sources.Count,
                scannedCassettes,
                editors.ConfiguredEditors,
                editors.RegisteredEditors,
                editors.UnregisteredEditors,
                editors.ValidEditorFogs,
                editors.UnassignedWritableFogs);

            ProjectIndexRebuildResult rebuild = await indexCoordinator.RebuildFromSourcesAsync(
                project,
                sources,
                cancellationToken);

            logger.LogInformation(
                "Full project refresh completed. Generation {GenerationId}; resources: {ResourceCount}; " +
                "triples: {TripleCount}; name search rows: {NameSearchRowCount}; " +
                "word search rows: {WordSearchRowCount}.",
                rebuild.GenerationId,
                rebuild.Statistics.Resources,
                rebuild.Statistics.Triples,
                rebuild.Statistics.NameSearchRows,
                rebuild.Statistics.WordSearchRows);

            return new ProjectFullRefreshResult(
                rebuild.GenerationId,
                enabledCassettes,
                scannedCassettes,
                sources.Count,
                editors,
                rebuild.Statistics);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Full project refresh failed for {ProjectId}.",
                project.ProjectId);
            throw;
        }
    }
}

public static class EditorFogAssignmentValidator
{
    public static EditorFogValidationStatistics Validate(
        ProjectDefinition project,
        IReadOnlyList<FogSourceDescriptor> sources,
        IdentityData identity,
        LocalAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(options);

        IdentityUser[] registeredEditors = identity.Users
            .Where(user => options.IsEditor(user.NormalizedLogin))
            .ToArray();
        HashSet<string> registeredLogins = registeredEditors
            .Select(user => user.NormalizedLogin)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> registeredEditorIds = registeredEditors
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);

        int unregisteredEditors = options.EditorLogins.Count(login =>
            !registeredLogins.Contains(login));
        int editorsWithFog = registeredEditors.Count(user => user.Fog is not null);
        int validEditorFogs = 0;
        List<string> invalidEditors = [];

        foreach (IdentityUser editor in registeredEditors)
        {
            if (EditorFogIsValid(project, sources, editor))
            {
                validEditorFogs++;
            }
            else
            {
                invalidEditors.Add(editor.Login);
            }
        }

        int unassignedWritableFogs = sources.Count(source =>
            source.Writable &&
            !source.IsCassetteMetadata &&
            !string.IsNullOrWhiteSpace(source.Owner) &&
            !registeredEditorIds.Contains(source.Owner));

        if (invalidEditors.Count > 0)
        {
            throw new InvalidOperationException(
                "The following registered editors have no matching writable Fog: " +
                string.Join(", ", invalidEditors.Order(StringComparer.Ordinal)) + ".");
        }

        return new EditorFogValidationStatistics(
            options.EditorLogins.Count,
            registeredEditors.Length,
            unregisteredEditors,
            editorsWithFog,
            validEditorFogs,
            registeredEditors.Length - validEditorFogs,
            unassignedWritableFogs);
    }

    private static bool EditorFogIsValid(
        ProjectDefinition project,
        IReadOnlyList<FogSourceDescriptor> sources,
        IdentityUser editor)
    {
        IdentityFogReference? fog = editor.Fog;
        if (fog is null)
        {
            return false;
        }

        CassetteDefinition? cassette = project.Cassettes.SingleOrDefault(value =>
            string.Equals(value.Id, fog.CassetteId, StringComparison.Ordinal));
        if (cassette is null || !cassette.Enabled || !cassette.AllowWrite)
        {
            return false;
        }

        string expectedPath;
        try
        {
            expectedPath = Path.GetFullPath(fog.RelativePath, cassette.Path);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        return sources.Any(source =>
            source.Writable &&
            !source.IsCassetteMetadata &&
            string.Equals(source.CassetteId, cassette.Id, StringComparison.Ordinal) &&
            string.Equals(source.Owner, editor.Id, StringComparison.Ordinal) &&
            PathsEqual(source.FogPath, expectedPath));
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
