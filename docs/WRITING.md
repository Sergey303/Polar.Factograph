# Fog writing

Polar.Factograph provides compatible append-only metadata writing through a guarded HTTP use case and a validated filesystem transaction.

## Routing

1. `ProjectWriteCassetteResolver` selects the requested cassette or the user's configured default.
2. The user must have `writeMetadata` for that cassette.
3. `FogWritableSourceSelector` selects a writable Fog in the cassette, preferring its metadata Fog and then a deterministic path order.
4. A source is writable only when the cassette permits writes and the Fog root contains both `prefix` and `counter`.

## Resource write

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

1. Acquire an exclusive same-Fog lock file.
2. Read the current root so a stale scanner snapshot cannot reuse an old counter.
3. Stream existing records one element at a time into a temporary file in the same directory.
4. Determine a timestamp newer than prior revisions of the same resource.
5. Append the complete new resource definition.
6. Flush the temporary file to disk.
7. Parse it again and verify the new revision and root counter.
8. Atomically replace the source Fog.
9. Delete the temporary file after any failure.

The original Fog remains unchanged until validation succeeds.

## Project transaction

`ProjectResourceWriteCoordinator` serializes writes and administrative rebuilds for one project index.

1. Repair a preceding `DIRTY` index before accepting another write.
2. Select the authorized cassette and writable Fog.
3. Create `DIRTY` before changing the source of truth.
4. Commit the validated Fog transaction.
5. Rebuild the complete Polar.DB generation without request cancellation.
6. Clear `DIRTY` only after the new generation becomes current.

Reads started after `DIRTY` appears return `503` instead of silently using stale derived data. Requests already holding an immutable completed generation may finish normally.

## Compatibility

Identifier generation, counter updates, UTC timestamps, empty literal removal, language values, datatype values, and resource links follow the legacy `FDataService.PutItem` behavior.

Two deliberate corrections are applied:

- previous resource definitions remain as append-only history and are resolved by the existing latest-`mT` materializer;
- direct `XElement.Save` is replaced by validated temporary-file replacement.

## Current boundary

`POST /api/resources` now exposes complete metadata definitions. Successful source write plus rebuild returns `201`. When source writing succeeds but rebuild fails, the API returns `202`, keeps `DIRTY`, and requires a successful rebuild before reads resume.

Document uploads, collection mutation, delete/substitute commands, and incremental index updates remain separate later increments.
