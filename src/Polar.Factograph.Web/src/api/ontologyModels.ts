export interface OntologyWriteOption {
  value: string;
  label: string;
}

export interface OntologyWriteProperty {
  id: string;
  label: string;
  kind: "literal" | "resource";
  ranges: string[];
  options: OntologyWriteOption[];
}

export interface OntologyWriteClass {
  id: string;
  label: string;
  parentClassId: string | null;
  properties: OntologyWriteProperty[];
}

export interface OntologyWriteSchema {
  classes: OntologyWriteClass[];
}