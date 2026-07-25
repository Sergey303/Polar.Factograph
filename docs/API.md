# API

The API is a thin layer over validated project configuration, effective access snapshots, and one completed Polar.DB generation.

## Identity

Production requests require an authenticated principal. The user id is read from `ClaimTypes.NameIdentifier`, `sub`, or `Identity.Name` in that order.

The API never accepts a user id from a query parameter or custom request header.

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
GET /api/resources/portrait?id={rdf-id}
GET /api/search/names?q={text}&limit=50&lang=ru
GET /api/search/words?q={text}&limit=50&lang=ru
GET /api/documents/location?uri={iiss-uri}
GET /api/documents/content?uri={iiss-uri}&variant={variant}
```

Document variants are `original`, `small`, `medium`, and `normal`. The metadata route returns availability flags and never exposes local filesystem paths. The content route supports HTTP range requests.

`/api/project` returns a safe overview: project identity, effective project rights, readable cassettes, and the default write cassette. It does not expose project members, role definitions, or filesystem paths.

Portrait and document reads require the project `read` right. Search requires both `read` and `search`. Cassette visibility is derived from the access snapshot inside the server. An unknown and a forbidden document both return 404.

## Administrative routes

```text
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
POST /api/admin/index/rebuild
```

All administrative routes above require `rebuildIndex`.

A rebuild streams Fog/XML, resolves current records, writes all four Polar.DB sets, builds their external indexes, and switches `CURRENT` only after the complete generation succeeds.

## Error boundary

The API uses stable error codes:

- `authentication_required` — 401;
- `forbidden` — 403;
- `resource_not_found`, `document_not_found`, or `document_variant_not_found` — 404;
- `invalid_request` — 400;
- `project_unavailable` or `storage_unavailable` — 503;
- `internal_error` — 500.
