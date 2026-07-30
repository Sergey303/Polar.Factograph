# Collections

Collection membership follows the established Factoria RDF model. An item is not linked directly to a collection. A separate `collection-member` resource contains two links:

```text
membership -- collection-item --> item
membership -- in-collection --> collection
```

The compatibility sources are `OpenA/App_Code/GetCollectionPath.cs` and the collection editing flow in `SypBlazor/Pages/Index0.razor` from `agmarchuk/Factoria`.

## Read API

```text
GET /api/collections/items?id={collection-id}&limit=100&lang=ru
```

The route returns the visible current items of one collection. Every reader receives:

- the item resource id;
- display name;
- RDF type and ontology label.

Membership details are capability-shaped:

- a read-only viewer receives `membershipResourceId: null` and `membershipCassetteId: null`;
- a user with effective `delete` access in the membership source cassette receives the membership resource id and that cassette's logical id, which are required by the remove request;
- an administrator with `rebuildIndex` receives the same logical deletion reference.

The response never contains the target resource cassette id, Fog path, local path, or other storage provenance. The internal application model retains membership and target provenance for visibility filtering, deterministic ordering, and guarded deletion.

`lang` defaults to `ru`. `limit` defaults to 100 and accepts values from 1 through 500.

## Mutation API

```text
POST /api/collections/items
POST /api/collections/items/remove
```

Add request:

```json
{
  "collectionId": "collection-1",
  "resourceId": "person-1",
  "cassetteId": "optional-explicit-cassette"
}
```

The add route writes a new `collection-member` through the normal ontology-validated resource pipeline. It requires `writeMetadata`. The collection and item must exist, be readable, and satisfy the ontology ranges of `in-collection` and `collection-item`.

Remove request:

```json
{
  "membershipResourceId": "membership-1",
  "collectionId": "collection-1",
  "resourceId": "person-1",
  "cassetteId": "optional-explicit-cassette"
}
```

The remove route requires `delete`. Under the project mutation gate it verifies that the visible current membership is a `collection-member` containing both requested links. It then appends a delete directive for the membership resource only; the collection and item remain unchanged.

The React client offers removal only when both the collection item response and the safe project overview expose the required cassette capability. The server remains authoritative and repeats every authorization and membership check.

Add returns `201 Created` and remove returns `200 OK` when the rebuilt generation is ready. Either route returns `202 Accepted` when Fog was committed but rebuild failed.

## Access boundary

Read requires project `read`. The server derives readable cassette ids from the effective access snapshot.

The collection, membership relation, `collection-item` link, and target resource must all be visible. A missing or forbidden collection returns the same `404 collection_not_found` response. Forbidden target items are omitted from the result.

A hidden or mismatched membership is reported as an invalid membership without disclosing which checked component was inaccessible or different.

## Ordering

Items are ordered deterministically by display name, resource id, and membership resource id. The limit is applied after inaccessible resources are removed and summaries are resolved.
