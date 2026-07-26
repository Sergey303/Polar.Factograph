# Fog writing

Polar.Factograph provides compatible append-only metadata mutations through guarded HTTP use cases and validated filesystem transactions.

## Routing

1. Resource definitions require `writeMetadata`.
2. Delete directives require `delete`.
3. Substitute directives require `substitute`.
4. The requested cassette or effective default cassette must grant the exact required right.
5. `FogWritableSourceSelector` prefers the cassette metadata Fog and then deterministic path order.

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

## Ontology validation

After `writeMetadata` authorization and before project locking, resource definitions are checked against the cached current ontology.

The validator checks:

- class existence;
- property existence;
- inherited property domains;
- datatype versus object-property value kind;
- enumeration state values;
- absence of language/datatype metadata on resource links.

Local names and full Fog namespace identifiers resolve to the same ontology term. Failed validation raises an invalid-request response before `DIRTY`, temporary files, or Fog changes. Object target existence and target-class range compatibility remain later checks.

## Delete and substitute

`FileSystemFogDirectiveWriter` appends one directive and never rewrites prior logical history.

Delete is serialized as a Fog `delete` record with `rdf:about`. Substitute is serialized with `old-id` and `new-id`. Both receive a UTC `mT`; the root counter remains textually unchanged.

A substitute source and target must differ after legacy `|` cleanup. Delete and substitute rights are independent.

## Filesystem transaction

1. Acquire an exclusive same-Fog lock file.
2. Read the current root under the lock.
3. Stream existing records one element at a time into a temporary file in the same directory.
4. Append the complete resource definition or directive.
5. Flush the temporary file to disk.
6. Parse it again and verify the exact appended record and root counter.
7. Atomically replace the source Fog.
8. Delete the temporary file after any failure.

The original Fog remains unchanged until validation succeeds.

## Project transaction

`ProjectFogMutationRunner` is shared by resource, delete, and substitute coordinators.

1. Serialize mutations and administrative rebuilds for one project index.
2. Repair a preceding `DIRTY` index before accepting another mutation.
3. Select the authorized cassette and writable Fog.
4. Create `DIRTY` before changing the source of truth.
5. Commit the validated Fog transaction.
6. Rebuild the complete Polar.DB generation without request cancellation.
7. Clear `DIRTY` only after the new generation becomes current.

Reads started after `DIRTY` appears return `503` instead of silently using stale derived data. Requests already holding an immutable completed generation may finish normally.

## Compatibility

Identifier generation, counter handling, UTC timestamps, empty literal removal, language values, datatype values, resource links, delete, and substitute formats follow the legacy behavior.

Two deliberate corrections are applied:

- previous resource definitions remain as append-only history and are resolved by the existing latest-`mT` materializer;
- direct `XElement.Save` is replaced by validated temporary-file replacement.

## Current boundary

Resource, delete, and substitute mutations are exposed through the API and rebuild Polar.DB before reporting an index-ready result. A committed Fog mutation with failed rebuild returns `202`, keeps `DIRTY`, and blocks stale reads.

Document uploads, collection mutation, object-range target validation, and incremental index updates remain separate later increments.
