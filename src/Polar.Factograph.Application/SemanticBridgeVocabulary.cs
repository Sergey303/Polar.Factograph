namespace Polar.Factograph.Application;

internal static class SemanticBridgeVocabulary
{
    public const string Name = "http://fogid.net/o/name";
    public const string Uri = "http://fogid.net/o/uri";

    public const string SystemObject = "http://fogid.net/o/sys-obj";
    public const string Organization = "http://fogid.net/o/org-sys";
    public const string Collection = "http://fogid.net/o/collection";
    public const string Document = "http://fogid.net/o/document";
    public const string PhotoDocument = "http://fogid.net/o/photo-doc";

    public const string Reflection = "http://fogid.net/o/reflection";
    public const string Reflected = "http://fogid.net/o/reflected";
    public const string InDocument = "http://fogid.net/o/in-doc";

    public const string Participation = "http://fogid.net/o/participation";
    public const string Participant = "http://fogid.net/o/participant";
    public const string InOrganization = "http://fogid.net/o/in-org";
    public const string Role = "http://fogid.net/o/role";

    public const string CollectionMember = "http://fogid.net/o/collection-member";
    public const string InCollection = "http://fogid.net/o/in-collection";
    public const string CollectionItem = "http://fogid.net/o/collection-item";

    public static readonly IReadOnlySet<string> TechnicalTypes = new HashSet<string>(
        [Reflection, Participation, CollectionMember],
        StringComparer.Ordinal);
}
