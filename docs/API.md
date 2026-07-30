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

## Public read routes

```text
GET /api/system/health
GET /api/project
GET /api/resources/page?id={rdf-id}&lang=ru
GET /api/search/names?q={text}&limit=50&lang=ru
GET /api/search/words?q={text}&limit=50&lang=ru
GET /api/search/classes?q={text}&limit=8&lang=ru
GET /api/search/by-type?type={class-id}&offset=0&limit=50&lang=ru
GET /api/collections/items?id={collection-id}&limit=100&lang=ru
GET /api/documents/location?uri={iiss-uri}
GET /api/documents/content?uri={iiss-uri}&variant={variant}
GET /api/documents/image?uri={iiss-uri}
```

The semantic page route returns the canonical public entity plus ontology-grouped links, timeline data, media attachments, and legacy compatibility arrays. `lang` defaults to `ru`.

Ordinary name/word search and ontology class search are separate contracts. Class suggestions never displace a better-ranked entity result. `/api/search/by-type` expands descendants of the selected class and returns one authorized, sorted page of entity summaries.

The internal search model keeps the matched predicate, language, source cassette, and ranking diagnostics. The public name/word response returns only the distinct visible matched literal values needed for the result snippet. Raw predicate identifiers and evidence language metadata do not leave the application layer.

Collection browsing follows the legacy membership-resource join and is documented in [COLLECTIONS.md](COLLECTIONS.md). A viewer receives only the visible item identity and presentation. Membership resource and cassette identifiers are returned only when the user has effective `delete` access for that membership or has `rebuildIndex`.

Document variants are `original`, `icon`, `small`, `medium`, and `normal`. `icon` is optional and falls back to `small` when a cassette has no dedicated icon file. The content route supports HTTP range requests. The image route selects the best authorized image representation in the order `normal`, `medium`, `small`, `icon`, then an image original; it does not return a non-image original as an Open Graph image.

`/api/documents/location` is capability-shaped:

- every authorized reader receives only availability flags for the original and preview variants;
- a user with effective `replaceDocuments` access in the source cassette receives its logical id and display name so the editor can authorize replacement;
- a user with `rebuildIndex` also receives that logical cassette reference;
- the response never includes local paths, the repeated document URI, or internal folder/document slot numbers.

`/api/project` returns a capability-shaped workspace overview rather than the raw access snapshot:

- every reader receives only the stable project id, project name, and `canAdmin` capability;
- a read-only viewer receives no cassette list and no default write cassette;
- an editor receives only readable cassettes whose project configuration and effective access snapshot both allow writing, together with only the cassette rights used by write operations;
- an administrator with `rebuildIndex` receives all readable cassette rights needed by the administration workspace;
- the default write cassette is returned only when it is present in the exposed cassette list.

The response does not expose the current user id, the raw project-right set, project members, role definitions, or filesystem paths.

The public health response reports the service as `ok` or `degraded` and exposes only whether preview processing is enabled plus its coarse state. Disabled preview processing is healthy. A failed, stopped, or unresponsive enabled worker makes the service status `degraded`, but the response never includes timestamps, filesystem paths, process output, or exception text.

## Editor read routes

```text
GET /api/ontology/write-schema?lang=ru
GET /api/resources/portrait?id={rdf-id}&lang=ru
GET /api/search/duplicates?type={class-id}&predicate={property-id}&value={text}&limit=10&lang=ru
```

These routes require project `read` plus at least one enabled, readable cassette with effective `AllowWrite=true` and `writeMetadata`.

The write-schema route returns localized classes and inherited writable properties. Each property contains its stable id, display label, literal/resource kind, range identifiers, and localized enumeration choices. It never returns the ontology path, raw XML, members, roles, or cassette configuration.

The raw portrait route supports editors that must preserve complete literals and direct links when appending a new revision. The public page uses `/api/resources/page` instead.

Duplicate suggestions are an editor preflight used before creating a resource. They remain restricted to the requested class or its descendants and also require normal search access.

Resource provenance is capability-shaped rather than returned uniformly:

- a read-only viewer receives `provenance: null` through the semantic page;
- a user with `writeMetadata` in the resource's source cassette receives only that cassette's logical id, which lets the editor choose the correct revision target;
- a user with `rebuildIndex` receives the full source record id, Fog source, and modification time.

Presented incoming links do not expose source cassette ids. Internal application and storage models retain cassette provenance for authorization, sorting, diagnostics, and write routing.

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

Delete request:

```json
{
  "resourceId": "person-1",
  "cassetteId": "optional-explicit-cassette"
}
```

Substitute request:

```json
{
  "oldResourceId": "person-1",
  "newResourceId": "person-2",
  "cassetteId": "optional-explicit-cassette"
}
```

Delete requires the cassette `delete` right. Substitute requires the independent cassette `substitute` right. A substitute source and target must differ after legacy `|` cleanup.

## Collection mutations

```text
POST /api/collections/items
POST /api/collections/items/remove
```

Add creates a new ontology-validated `collection-member` linking the requested collection and item. It requires `writeMetadata`, and both target resources must be readable and satisfy the ontology ranges.

Remove requires `delete`. Before creating `DIRTY`, it verifies that the visible current membership has type `collection-member` and contains both requested links. Only that membership is deleted; the collection and item remain unchanged.

Add returns `201 Created` and remove returns `200 OK` when the new generation is ready. The complete request and response contracts are documented in [COLLECTIONS.md](COLLECTIONS.md).

All metadata mutation routes use the same project transaction: serialize mutations, repair a preceding dirty index, validate current targets, append one validated Fog record, rebuild Polar.DB, and switch `CURRENT` only after the generation is complete.

- create/add routes return `201 Created` when the index is ready;
- delete/substitute/remove routes return `200 OK` when the index is ready;
- any metadata mutation returns `202 Accepted` when Fog was committed but rebuild failed;
- while `DIRTY` remains, reads return `503` instead of exposing stale derived data.

Mutation responses never expose local Fog paths.

## Administrative routes

```text
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
GET  /api/admin/project/ontology-validation
GET  /api/admin/index/status
POST /api/admin/index/rebuild
GET  /api/admin/previews/status
```

All administrative routes above require `rebuildIndex`.

The ontology-validation response contains term, error, and warning counts plus ordered issues with stable codes and ontology term identifiers. Validation parses raw terms before constructing the strict runtime catalog, so missing parents and inheritance cycles can be reported even though normal ontology loading rejects them. A missing, unreadable, or malformed ontology document is returned as one fatal report issue rather than an unhandled exception. Details are documented in [ONTOLOGY_VALIDATION.md](ONTOLOGY_VALIDATION.md).

The index status response does not expose filesystem paths. It reports `ready`, `dirty`, `missing`, or `invalid`, the `DIRTY` timestamp when parseable, the current generation id and availability, and counts of completed and `.building` generations. An invalid or missing `CURRENT` pointer is returned as diagnostic state instead of causing an unhandled error.

The preview status response contains queue counts and oldest queued time per cassette together with the worker runtime snapshot: start/stop and cycle timestamps, last and total handled counts, consecutive failures, and a fixed failure code. It also includes the evaluated state `disabled`, `starting`, `working`, `idle`, `degraded`, `unresponsive`, or `stopped`. It never exposes queue directories, original paths, process output, or exception text.

A rebuild streams Fog/XML, resolves current records, writes all four Polar.DB sets, builds their external indexes, switches `CURRENT` only after the complete generation succeeds, and clears `DIRTY` only after success.

## Error boundary

The API uses stable error codes:

- `authentication_required` — 401;
- `forbidden` — 403;
- `resource_not_found`, `collection_not_found`, `document_not_found`, `document_variant_not_found`, or `document_image_not_found` — 404;
- `invalid_request` — 400;
- `project_unavailable` or `storage_unavailable` — 503;
- `internal_error` — 500.

Authorization failures return a neutral `forbidden` message. User identifiers, role/right names, cassette details, and exception text remain in server logs only. Project-runtime and storage failures likewise use stable neutral 503 messages and never return local paths, generation identifiers, Fog filenames, or raw `IOException` text.
