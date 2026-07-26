# API

The API is a thin layer over validated project configuration, effective access snapshots, Fog/XML source files, and one completed Polar.DB generation.

## Identity

Production requests require an authenticated principal. The user id is read from `ClaimTypes.NameIdentifier`, `sub`, or `Identity.Name` in that order.

The API never accepts a user id from a query parameter or custom request header. JWT bearer setup and development fallback rules are documented in [AUTHENTICATION.md](AUTHENTICATION.md).

For local development only, `appsettings.Development.json` may provide:

```json
{
  "Api": {
    "DevelopmentUserId": "admin"
  }
}
```

## Read routes

```text
GET /api/system/health
GET /api/project
GET /api/resources/portrait?id={rdf-id}&lang=ru
GET /api/search/names?q={text}&limit=50&lang=ru
GET /api/search/words?q={text}&limit=50&lang=ru
GET /api/collections/items?id={collection-id}&limit=100&lang=ru
GET /api/documents/location?uri={iiss-uri}
GET /api/documents/content?uri={iiss-uri}&variant={variant}
```

The portrait route returns ontology labels, inverse labels, enumeration display values, and ontology property order while preserving raw RDF identifiers and literal values. `lang` defaults to `ru`.

Collection browsing follows the legacy membership-resource join and is documented in [COLLECTIONS.md](COLLECTIONS.md).

Document variants are `original`, `small`, `medium`, and `normal`. The metadata route returns availability flags and never exposes local filesystem paths. The content route supports HTTP range requests.

`/api/project` returns a safe overview: project identity, effective project rights, readable cassettes, and the default write cassette. It does not expose project members, role definitions, or filesystem paths.

Portrait, collection, and document reads require the project `read` right. Search requires both `read` and `search`. Cassette visibility is derived from the access snapshot inside the server.

## Resource write

```text
POST /api/resources
```

The request contains a complete append-only resource definition:

```json
{
  "typeId": "person",
  "resourceId": "optional-existing-id",
  "cassetteId": "optional-explicit-cassette",
  "properties": [
    { "predicate": "name", "value": "Alice", "language": "en" },
    { "predicate": "friend", "value": "person-2", "kind": "resource" }
  ]
}
```

`kind` is `literal` by default and may be `resource`. When `cassetteId` is omitted, the effective default write cassette is used. The authenticated member must have `writeMetadata` for the selected cassette.

The API serializes project mutations, repairs a preceding dirty index before accepting another write, atomically appends the complete Fog definition, and then rebuilds Polar.DB without observing request cancellation after the source commit.

- `201 Created` means Fog was written and a complete new index generation became current.
- `202 Accepted` means Fog was written, but rebuild failed. `indexReady` is `false`, the project remains marked `DIRTY`, and reads return `503` until a successful rebuild.

The response never exposes the local Fog path.

## Administrative routes

```text
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
POST /api/admin/index/rebuild
```

All administrative routes above require `rebuildIndex`.

A rebuild streams Fog/XML, resolves current records, writes all four Polar.DB sets, builds their external indexes, switches `CURRENT` only after the complete generation succeeds, and clears `DIRTY` only after success.

## Error boundary

The API uses stable error codes:

- `authentication_required` — 401;
- `forbidden` — 403;
- `resource_not_found`, `collection_not_found`, `document_not_found`, or `document_variant_not_found` — 404;
- `invalid_request` — 400;
- `project_unavailable` or `storage_unavailable` — 503;
- `internal_error` — 500.
