# Legacy implementation sources

Polar.Factograph treats the existing `agmarchuk` repositories as the behavioral reference for the legacy cassette and Fog/XML formats. New code is separated into modern components and verified by compatibility tests instead of copying the old monolithic services wholesale.

## Current mappings

| Polar.Factograph component | Legacy source | Preserved behavior | Intentional change |
|---|---|---|---|
| `FileSystemFogSourceScanner` | [`Factograph.Data/FDataService.cs`](https://github.com/agmarchuk/Factoria/blob/main/Factograph.Data/FDataService.cs) | `{cassette}_current.fog`, additional Fog files in four-character `originals` directories, writable only with cassette permission plus `prefix` and `counter` | Streams only the XML root and reports explicit validation errors |
| `FileSystemFogRecordReader` | `FDataService.FillDb`, `DAdapter.FillDb0` | Removes `|` from identifiers, maps record and predicate local names to `http://fogid.net/o/`, preserves `xml:lang`, converts `iisstore/@uri` to the `uri` field, accepts anonymous RDF resource records | Reads one record at a time; replaces the legacy shared `no ident` value with a deterministic unique skolem URI based on cassette, logical Fog path, record position, and type |
| `LegacyFogProjectMaterializer` | `FDataService.FillDb`, `UpiAdapter.LoadXFlow` | Global `delete` and `substitute`, closed substitution chains, latest duplicate definition by `mT`, first definition wins equal maximum `mT`, substitutions applied to object references, deleted references remain dangling | Detects substitution cycles and reports them instead of recursing indefinitely |
| synthetic cassette root | `FDataService.FillDb` | Adds `cassetterootcollection` with the name `кассеты` | Does not add a duplicate synthetic record when a current source definition already exists |
| `CassetteDocumentPathResolver` | `FDataService.CassDirPath`, `GetOriginalPath`, `GetFilePath` | Resolves the cassette name from `iiss://`, uses the final four-character folder and document number, searches `originals` and `documents/{small,medium,normal}` | Rejects unsafe path parts and ambiguous multiple files instead of selecting an arbitrary filesystem result |
| `ProjectResourcePortraitService` | `FDataService.GetItemByIdBasic`, `GetBasicPortrait` | Builds one portrait from literal fields, direct RDF links, and inverse RDF links | Applies cassette visibility before reading triples, exposes provenance, and returns a deterministic ordering |
| `XmlOntologyCatalogLoader` | `RXOntology` | Loads classes, datatype/object properties, labels, inverse labels, priorities, class inheritance, domain/range, and enumeration states | Rejects duplicate identifiers and cyclic class hierarchies explicitly; language fallback is deterministic |
| `OntologyResourcePortraitPresenter` | `RXOntology.LabelOfOnto`, `InvLabelOfOnto`, `GetDirectPropsByType`, `GetInversePropsByType` | Labels types and properties, translates enumeration values, and orders direct/inverse properties by ontology priority and inheritance | Keeps raw identifiers and values alongside display text and safely falls back when ontology terms are unknown |
| `LegacySearchIndexProjector`, `ProjectResourceSearchService` | `UpiAdapter` name and word indexes, `SearchByName`, `SearchByWords` | Uses `name`/`alias` for name search; `name`/`alias`/`description`/`doc-content` for word search; ranks word results by matched-word count | Materializes exact prefix/word keys for `DbSet` external indexes, uses invariant Unicode normalization, ignores repeated query words for scoring, enforces cassette visibility, and bounds result enrichment |

Anonymous resource support applies only to normal RDF resource records. Identifier-dependent directives such as `delete` and `substitute` still require explicit identifiers and fail validation when those identifiers are absent.

## Compatibility rule

For every migrated behavior:

1. identify the exact legacy implementation;
2. describe the observable rule rather than inherit accidental class structure;
3. implement it in a focused component;
4. test synthetic edge cases;
5. test at least one real unchanged cassette;
6. document any deliberate correction separately.

Fog/XML and the existing cassette filesystem remain authoritative. Polar.DB receives only the resolved current project cloud and can always be rebuilt from these sources.
