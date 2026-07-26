using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectDocumentAddCoordinator(
    ProjectCassetteCommandResolver cassetteResolver,
    ProjectOperationGate operationGate,
    ICassetteDocumentWriter writer,
    ICassettePreviewRequestWriter previewWriter,
    IOptions<DocumentUploadOptions> options)
{
    public async Task<DocumentBinaryWriteResponse> AddAsync(
        ProjectAccessContext context,
        Stream content,
        string fileName,
        string? requestedCassetteId,
        long? contentLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        long maxBytes = DocumentUploadRules.RequireLength(
            contentLength,
            options.Value.MaxUploadBytes);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            CassetteRights.AddDocuments,
            requestedCassetteId);
        CassetteDefinition cassette = ProjectCassetteDefinitionResolver.Require(
            context.Project,
            cassetteId);

        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            context.Project.Index.Path,
            cancellationToken);
        CassetteDocumentWriteResult result = await writer.AddAsync(
            cassette,
            content,
            fileName,
            maxBytes,
            cancellationToken);
        CassettePreviewQueueResult preview = await previewWriter.QueueAsync(
            cassette,
            result,
            cancellationToken);
        return DocumentBinaryWriteMapper.Map(result, preview);
    }
}
