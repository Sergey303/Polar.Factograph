# Fog writing

Polar.Factograph exposes compatible metadata writes while retaining Fog/XML as the source of truth and Polar.DB as rebuildable derived data.

## Routing

1. `ProjectRequestContextFactory` resolves the authenticated project user.
2. `ProjectWriteCassetteResolver` selects the requested cassette or the user's configured default.
3. The user must have `writeMetadata` for that cassette.
4. `FogWritableSourceSelector` selects a writable Fog in the cassette, preferring its metadata Fog and then a deterministic path order.
5. A source is writable only when the cassette permits writes and the Fog root contains both `prefix` and `counter`.

The API never accepts a user id or effective rights from the request body.

## Resource write

`POST /api/resources` generates an identifier. `PUT /api/resources/{resourceId}` appends a complete revision for an existing or explicit identifier.

`FileSystemFogResourceWriter` receives a complete resource definition. It does not patch individual XML fields in place.

For a generated identifier:

```text
resourceId = prefix + counter
counter = counter + 1
```

For an explicit identifier, the existing counter and its textual formatting are preserved.

The writer also:

- removes `|` from identifiers;
- writes `mT` in UTC with one-second precision;
- advances `mT` beyond the latest existing revision when the clock value is not newer;
- omits empty literal properties;
- preserves `xml:lang` and `rdf:datatype`;
- writes object properties through `rdf:resource`;
- appends a new definition instead of destroying prior source history.

The monotonic timestamp rule is required because equal maximum `mT` values intentionally resolve to the first definition.

## Filesystem transaction

1. Acquire the project mutation lease.
2. Resolve the permitted cassette and writable Fog.
3. Acquire an exclusive same-Fog lock file.
4. Read the current root so a stale scanner snapshot cannot reuse an old counter.
5. Stream existing records one element at a time into a temporary file in the same directory.
6. Determine a timestamp newer than prior revisions of the same resource.
7. Append the complete new resource definition.
8. Flush the temporary file to disk.
9. Parse it again and verify the new revision and root counter.
10. Atomically replace the source Fog.
11. Rebuild the complete Polar.DB project generation while retaining the project mutation lease.
12. Return only after the new `CURRENT` generation is complete.

The original Fog remains unchanged until source validation succeeds. The preceding Polar.DB generation remains active until the complete rebuild succeeds.

## Concurrent operations

Metadata writes and manual administrative rebuilds use the same project-level lease. This prevents an older concurrent rebuild from switching `CURRENT` after a newer source write has already been indexed.

The Fog writer also has a per-file cross-process lease. The narrower lock protects `prefix` and `counter`; the project lock protects source-to-index ordering.

## Refresh failure

A valid Fog write cannot truthfully be rolled back merely because rebuilding derived data failed. In that case:

- the new Fog revision remains authoritative;
- the preceding completed Polar.DB generation remains readable;
- the API returns `write_committed_index_refresh_failed` with status 503;
- an administrator must run `POST /api/admin/index/rebuild` before reads are expected to expose the change.

## Compatibility

Identifier generation, counter updates, UTC timestamps, empty literal removal, language values, datatype values, and resource links follow the legacy `FDataService.PutItem` behavior.

Two deliberate corrections are applied:

- previous resource definitions remain as append-only history and are resolved by the existing latest-`mT` materializer;
- direct `XElement.Save` is replaced by validated temporary-file replacement.

## Remaining write work

The current API writes complete metadata revisions. Document upload and replacement, collection mutations, `delete`, `substitute`, batch operations, optimistic concurrency tokens, and incremental index mutation remain separate increments.
