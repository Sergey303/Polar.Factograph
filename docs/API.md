# API

The API is a thin layer over validated project configuration, effective access snapshots, Fog/XML source files, and one completed Polar.DB generation.

## Identity

Production requests require an authenticated principal. The user id is read from `ClaimTypes.NameIdentifier`, `sub`, or `Identity.Name` in that order.

The API never accepts a user id from a query parameter or custom request header. JWT bearer setup, browser Authorization Code with PKCE, and development fallback rules are documented in [AUTHENTICATION.md](AUTHENTICATION.md).

The unauthenticated browser configuration route is:

```text
GET /api/auth/browser
```

It returns only whether browser login is enabled plus the public `authority`, `clientId`, and `scope`. It never returns a client secret, signing key, users, roles, or project data.

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
GET /api/ontology/write-schema?lang=ru
GET /api/resources/portrait?id={rdf-id}&lang=ru
GET /api/search/names?q={text}&limit=50&lang=ru
GET /api/search/words?q={text}&limit=50&lang=ru
GET /api/collections/items?id={collection-id}&limit=100&lang=ru
GET /api/documents/location?uri={iiss-uri}
GET /api/documents/content?uri={iiss-uri}&variant={variant}
```

The portrait route returns ontology labels, inverse labels, enumeration display values, and ontology property order while preserving raw RDF identifiers and literal values. `lang` defaults to `ru`.

The ontology write-schema route returns localized classes and their inherited writable properties. Each property contains its stable id, display label, literal/resource kind, range identifiers, and localized enumeration choices. It requires project `read` and never returns the ontology path, raw XML, members, roles, or cassette configuration.

Collection browsing follows the legacy membership-resource join and is documented in [COLLECTIONS.md](COLLECTIONS.md).

Document variants are `original`, `small`, `medium`, and `normal`. The metadata route returns availability flags and never exposes local filesystem paths. The content route supports HTTP range requests.

`/api/project` returns a safe overview: project identity, effective project rights, readable cassettes, and the default write cassette. It does not expose project members, role definitions, or filesystem paths.

Portrait, ontology schema, collection, and document reads require the project `read` right. Search requires both `read` and `search`. Cassette visibility is derived from the access snapshot inside the server.

The public health response reports the service as `ok` or `degraded` and exposes only whether preview processing is enabled plus its coarse state. Disabled preview processing is healthy. A failed, stopped, or unresponsive enabled worker makes the service status `degraded`, but the response never includes timestamps, filesystem paths, process output, or exception text.

## Document binary write

```text
POST /api/documents/files?fileName={name.ext}&cassetteId={optional-id}
PUT  /api/documents/files?uri={iiss-uri}&fileName={name.ext}
```

The request body is the raw binary stream. Add requires `addDocuments`; replace requires the independent `replaceDocuments` right. New files receive the next compatible four-character folder/document pair and return an `iiss://` URI. Replacement preserves that URI and requires the same extension as the existing original.

Files are streamed to a temporary path, size-limited, hashed with SHA-256, flushed, and atomically renamed. Binary-only operations do not change Fog or rebuild Polar.DB. Creating or updating the RDF document description continues through `POST /api/resources`.

After the original is committed, the API attempts to persist a durable preview-generation request. The response reports `previewState` as `queued` or `queue-failed`; a queued response also includes `previewRequestId` and `previewQueuedAtUtc`. A queue failure does not make the already committed binary write fail, which prevents unsafe client retries.

The complete contract, configuration, preview queue format, and metadata workflow are documented in [DOCUMENT_WRITING.md](DOCUMENT_WRITING.md).

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

Supplying an existing `resourceId` appends a complete new revision; it is not a partial patch. A client editor must therefore send every literal and direct resource link that should remain current. The React workspace builds this request from the complete authorized portrait. Values outside the current write schema remain visible, but the client blocks saving instead of silently dropping them or sending a request that the server must reject.

After authorization and before project locking or `DIRTY`, the request is checked against the current ontology:

- `typeId` must identify a class;
- every property must be available for that class through its domain or an inherited domain;
- datatype properties require literal values;
- object properties require resource values and cannot carry language/datatype metadata;
- values backed by an ontology enumeration must be declared states.

After any preceding `DIRTY` generation is repaired and while the mutation gate is held, external object targets are checked against the current Polar.DB generation:

- the target must exist in a cassette readable by the current user;
- at least one target `rdf:type` must satisfy an object-property `range`, including class inheritance;
- multiple target types are supported;
- a hidden target is reported the same way as a missing target;
- an explicit self-reference is checked against the request `typeId` and does not require an older generation.

Local Fog names and full `http://fogid.net/o/` identifiers are accepted for ontology terms.

## Delete and substitute

```text
POST /api/resources/delete
POST /api/resources/substitute
```

Delete appends a Fog `delete` directive. Substitute appends a Fog `substitute` directive. Both require the cassette `delete` right and trigger the shared write/index refresh pipeline.

## Collection mutations

```text
POST /api/collections/items
POST /api/collections/items/remove
```

Adding creates a compatible membership resource and requires `writeMetadata` for the selected write cassette. Removing appends a delete directive for the membership record itself and requires `delete` on that membership's actual source cassette.

## Administrative routes

```text
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
GET  /api/admin/index/status
POST /api/admin/index/rebuild
GET  /api/admin/previews/status
```

All administrative routes require the project `rebuildIndex` right. The React dashboard intentionally uses only aggregate status, materialization summary, rebuild, and preview status; it does not request or display physical source paths.
