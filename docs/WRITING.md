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

The first validation stage checks:

- class existence;
- property existence;
- inherited property domains;
- datatype versus object-property value kind;
- enumeration state values;
- absence of language/datatype metadata on resource links.

After the mutation gate is acquired and a preceding `DIRTY` index is repaired, external object targets are checked against the current generation before a new `DIRTY` marker is created.

The target stage checks:

- existence in a cassette readable by the current user;
- all declared `rdf:type` values;
- object-property ranges with class inheritance;
- explicit self-references against the request type.

A hidden target is indistinguishable from a missing target. Literal-only requests and explicit self-references do not require an older Polar.DB generation. Any failed validation leaves Fog and `DIRTY` unchanged.

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
3. Validate current object targets while the repaired generation is stable.
4. Select the authorized cassette and writable Fog.
5. Create `DIRTY` before changing the source of truth.
6. Commit the validated Fog transaction.
7. Rebuild the complete Polar.DB generation without request cancellation.
8. Clear `DIRTY` only after the new generation becomes current.

Reads started after `DIRTY` appears return `503` instead of silently using stale derived data. Requests already holding an immutable completed generation may finish normally.

## Compatibility

Identifier generation, counter handling, UTC timestamps, empty literal removal, language values, datatype values, resource links, delete, and substitute formats follow the legacy behavior.

Two deliberate corrections are applied:

- previous resource definitions remain as append-only history and are resolved by the existing latest-`mT` materializer;
- direct `XElement.Save` is replaced by validated temporary-file replacement.

## Current boundary

Resource, delete, and substitute mutations are exposed through the API and rebuild Polar.DB before reporting an index-ready result. A committed Fog mutation with failed rebuild returns `202`, keeps `DIRTY`, and blocks stale reads.

Document uploads, collection mutation, incremental index updates, and the React client remain separate later increments.
