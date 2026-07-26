using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectDocumentReplaceCoordinator(
    CassetteDocumentPathResolver pathResolver,
    ProjectCassetteCommandResolver cassetteResolver,
    ProjectOperationGate operationGate,
    ICassetteDocumentWriter writer,
    IOptions<DocumentUploadOptions> options)
{
    public async Task<DocumentBinaryWriteResponse> ReplaceAsync(
        ProjectAccessContext context,
        string documentUri,
        Stream content,
        string fileName,
        long? contentLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUri);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        long maxBytes = DocumentUploadRules.RequireLength(
            contentLength,
            options.Value.MaxUploadBytes);
        CassetteDocumentLocation initial = pathResolver.Resolve(
            context.Project,
            documentUri);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            CassetteRights.ReplaceDocuments,
            initial.CassetteId);
        CassetteDefinition cassette = ProjectCassetteDefinitionResolver.Require(
            context.Project,
            cassetteId);

        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            context.Project.Index.Path,
            cancellationToken);
        CassetteDocumentLocation current = pathResolver.Resolve(
            context.Project,
            documentUri);
        CassetteDocumentWriteResult result = await writer.ReplaceAsync(
            cassette,
            current,
            content,
            fileName,
            maxBytes,
            cancellationToken);
        return DocumentBinaryWriteMapper.Map(result);
    }
}
