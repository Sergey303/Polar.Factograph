using Polar.Factograph.Storage;

namespace Polar.Factograph.Application.Tests;

internal static class CollectionTestData
{
    public const string InCollection = "http://fogid.net/o/in-collection";
    public const string CollectionItem = "http://fogid.net/o/collection-item";
    public const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    public const string Name = "http://fogid.net/o/name";

    public static ResourceHead Head(string id, string cassetteId) => new(
        id,
        Guid.NewGuid(),
        cassetteId,
        cassetteId + ".fog",
        DateTimeOffset.UnixEpoch,
        IsDeleted: false);

    public static TripleRow Link(
        string subject,
        string predicate,
        string target,
        string cassetteId) => new(
        Guid.NewGuid(),
        subject,
        predicate,
        TripleObjectKind.Iri,
        target,
        Language: null,
        DataType: null,
        Guid.NewGuid(),
        cassetteId,
        cassetteId + ".fog",
        DateTimeOffset.UnixEpoch);

    public static NameSearchHit NameHit(
        string resourceId,
        string value,
        string cassetteId) => new(
        resourceId,
        Name,
        value,
        "ru",
        cassetteId);
}
