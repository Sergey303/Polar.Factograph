# API

The API is a thin layer over validated project configuration, effective access snapshots, Fog/XML source files, and completed Polar.DB generations.

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

## Metadata write routes

```text
POST /api/resources
PUT  /api/resources/{resourceId}
```

`POST` generates an identifier from the selected Fog root's `prefix` and current `counter`. `PUT` appends a new complete revision for the path identifier. Both routes require `writeMetadata` for the selected cassette.

Example body:

```json
{
  "typeId": "http://fogid.net/o/person",
  "cassetteId": "current",
  "properties": [
    {
      "predicate": "http://fogid.net/o/name",
      "kind": "literal",
      "value": "Alice",
      "language": "en"
    },
    {
      "predicate": "http://fogid.net/o/friend",
      "kind": "resource",
      "value": "person-2"
    }
  ]
}
```

`cassetteId` is optional. When omitted, the user's configured default write cassette is used. Property kind is `literal` or `resource`.

A successful response contains the logical resource id, cassette id, UTC modification time, completed generation id, source-file count, and rebuild statistics. It never contains a Fog path or another server filesystem path.

The API serializes project mutations. One lock covers the Fog transaction and the full project-index rebuild. A successful response therefore means the new source revision and the new `CURRENT` generation are both complete.

## Administrative routes

```text
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
POST /api/admin/index/rebuild
```

All administrative routes above require `rebuildIndex`. Manual rebuild uses the same project mutation lock as metadata writes.

A rebuild streams Fog/XML, resolves current records, writes all four Polar.DB sets, builds their external indexes, and switches `CURRENT` only after the complete generation succeeds.

## Error boundary

The API uses stable error codes:

- `authentication_required` — 401;
- `forbidden` — 403;
- `resource_not_found`, `collection_not_found`, `document_not_found`, or `document_variant_not_found` — 404;
- `invalid_request` — 400;
- `project_unavailable` or `storage_unavailable` — 503;
- `write_committed_index_refresh_failed` — 503: Fog/XML contains the new revision, but the previous Polar.DB generation remains active; run the administrative rebuild before reading the change;
- `internal_error` — 500.
