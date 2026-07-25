# Collections

Collection membership follows the established Factoria RDF model. An item is not linked directly to a collection. A separate membership resource contains two links:

```text
membership -- collection-item --> item
membership -- in-collection --> collection
```

The compatibility source is `OpenA/App_Code/GetCollectionPath.cs` from `agmarchuk/Factoria`.

## API

```text
GET /api/collections/items?id={collection-id}&limit=100&lang=ru
```

The route returns the visible current items of one collection. Each item contains:

- the membership resource id;
- the item resource id;
- display name;
- RDF type and ontology label;
- membership cassette id;
- item cassette id.

`lang` defaults to `ru`. `limit` defaults to 100 and accepts values from 1 through 500.

## Access boundary

The request requires project `read`. The server derives readable cassette ids from the effective access snapshot.

The collection, membership relation, `collection-item` link, and target resource must all be visible. A missing or forbidden collection returns the same `404 collection_not_found` response. Forbidden target items are omitted from the result.

## Ordering

Items are ordered deterministically by display name, resource id, and membership resource id. The limit is applied after inaccessible resources are removed and summaries are resolved.
