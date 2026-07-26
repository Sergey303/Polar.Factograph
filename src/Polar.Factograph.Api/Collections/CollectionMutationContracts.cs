namespace Polar.Factograph.Api.Collections;

public sealed record CollectionItemAddRequest(
    string CollectionId,
    string ResourceId,
    string? CassetteId = null);

public sealed record CollectionItemRemoveRequest(
    string MembershipResourceId,
    string CollectionId,
    string ResourceId,
    string? CassetteId = null);

public sealed record CollectionItemMutationResponse(
    string MembershipResourceId,
    string CollectionId,
    string ResourceId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    bool IndexReady,
    Guid? GenerationId);
