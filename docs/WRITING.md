# Fog writing

Polar.Factograph now has the filesystem foundation for compatible metadata writes. It is intentionally separate from HTTP endpoints and Polar.DB refresh orchestration.

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

## Compatibility

Identifier generation, counter updates, UTC timestamps, empty literal removal, language values, datatype values, and resource links follow the legacy `FDataService.PutItem` behavior.

Two deliberate corrections are applied:

- previous resource definitions remain as append-only history and are resolved by the existing latest-`mT` materializer;
- direct `XElement.Save` is replaced by validated temporary-file replacement.

## Current boundary

The writer is not yet exposed through the API. A write changes the source of truth but does not incrementally update the active Polar.DB generation. The API write use case must trigger a successful rebuild before new reads are expected to observe the change.
