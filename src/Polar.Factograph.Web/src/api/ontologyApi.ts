import { requestJson } from "./http";
import type { OntologyWriteSchema } from "./ontologyModels";

export const ontologyApi = {
  getWriteSchema(
    token: string,
    signal?: AbortSignal
  ): Promise<OntologyWriteSchema> {
    return requestJson<OntologyWriteSchema>(
      "api/ontology/write-schema?lang=ru",
      token,
      signal
    );
  }
};
