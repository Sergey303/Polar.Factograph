# Ontology validation

Polar.Factograph uses the ontology both as a semantic schema and as the source of labels, relation groups, writable fields, and resource-picker constraints. A malformed ontology must therefore be diagnosable without allowing it into normal runtime services.

## Two-stage boundary

Ontology loading has two distinct stages:

1. `XmlOntologyCatalogLoader.LoadTermsAsync` parses XML into raw `OntologyTerm` values without constructing the class hierarchy.
2. `XmlOntologyCatalogLoader.LoadAsync` constructs the strict `OntologyCatalog`; missing parents and hierarchy cycles are rejected here.

Administrative validation runs between these stages. This allows the administrator to receive a structured report for a hierarchy that the normal application correctly refuses to use.

## Endpoint

```text
GET /api/admin/project/ontology-validation
```

The endpoint requires the `rebuildIndex` project right. It exposes internal ontology identifiers and is not part of the anonymous public API.

The response contains:

- `termCount`;
- `errorCount`;
- `warningCount`;
- `isValid`;
- ordered `issues` with `severity`, `code`, `termId`, and a Russian explanation.

The endpoint returns an `OntologyValidationReport` with HTTP 200 even when the ontology document is missing or malformed. The report is the successful result of the diagnostic operation; it does not claim that the ontology itself is valid.

## Errors

Errors mean that a normal semantic or editing operation cannot be defined safely.

Current error codes include:

- `missing_entity_root` — `http://fogid.net/o/sys-obj` is absent or is not a class;
- `missing_parent_class` — a class references a parent absent from the ontology;
- `parent_is_not_class` — `SubClassOf` points to a non-class term;
- `cyclic_class_hierarchy` — class inheritance contains a cycle;
- `missing_domain_class` / `domain_is_not_class`;
- `missing_range_class` / `range_is_not_class` for object properties;
- `no_concrete_entity_target` — an object-property range has no non-abstract descendant of `sys-obj`, so the universal resource picker cannot offer a valid entity;
- `ontology_file_not_found`;
- `ontology_file_unreadable`;
- `ontology_document_invalid` — malformed XML, duplicate identifiers, missing `rdf:about`, or another structural parse failure.

## Warnings

Warnings preserve a usable fallback but reduce interface quality or schema precision.

Current warning codes include:

- `missing_label` — the interface falls back to the raw URI;
- `missing_domain` — the property cannot be placed in a type-driven universal form;
- `missing_range` — a literal is edited as plain text, while an object property cannot constrain its picker;
- `missing_inverse_label` — incoming relations use the ordinary property label.

Unknown literal datatype ranges are not rejected merely because their URI is not declared as a local ontology term. Existing cassettes may use external or legacy datatype identifiers, and the universal editor can safely fall back to text.

## Administration UI

The administration dialog loads the report automatically and shows:

- a compact state badge;
- term, error, and warning counts;
- a bounded expandable issue list;
- exact issue codes and term identifiers for XML correction.

The top-level refresh action reloads both runtime status and ontology validation. Fog materialization remains an explicit on-demand operation because it can be substantially more expensive.
