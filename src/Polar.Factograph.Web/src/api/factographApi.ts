import { requestBlob, requestJson } from "./http";
import type {
  DocumentLocation,
  DocumentVariant,
  PotentialDuplicateResource,
  ProjectOverview,
  ResourcePortrait,
  ResourceSearchResult,
  SemanticResourcePage
} from "./models";
import { mergeSearchResults } from "./searchResults";

function query(parameters: Record<string, string | number>): string {
  const values = new URLSearchParams();
  for (const [name, value] of Object.entries(parameters)) {
    values.set(name, String(value));
  }
  return values.toString();
}

export function documentContentUrl(uri: string, variant: DocumentVariant): string {
  return `api/documents/content?${query({ uri, variant })}`;
}

export function documentImageUrl(uri: string): string {
  return `api/documents/image?${query({ uri })}`;
}

export const factographApi = {
  getProject(token: string, signal?: AbortSignal): Promise<ProjectOverview> {
    return requestJson<ProjectOverview>("api/project", token, signal);
  },

  async search(
    text: string,
    token: string,
    signal?: AbortSignal
  ): Promise<ResourceSearchResult[]> {
    const parameters = query({ q: text, limit: 50, lang: "ru" });
    const [names, words] = await Promise.all([
      requestJson<ResourceSearchResult[]>(
        `api/search/names?${parameters}`,
        token,
        signal
      ),
      requestJson<ResourceSearchResult[]>(
        `api/search/words?${parameters}`,
        token,
        signal
      )
    ]);
    return mergeSearchResults(names, words);
  },

  findPotentialDuplicates(
    type: string,
    predicate: string,
    value: string,
    token: string,
    signal?: AbortSignal
  ): Promise<PotentialDuplicateResource[]> {
    const parameters = query({ type, predicate, value, limit: 10, lang: "ru" });
    return requestJson<PotentialDuplicateResource[]>(
      `api/search/duplicates?${parameters}`,
      token,
      signal
    );
  },

  getPortrait(
    resourceId: string,
    token: string,
    signal?: AbortSignal
  ): Promise<ResourcePortrait> {
    const parameters = query({ id: resourceId, lang: "ru" });
    return requestJson<ResourcePortrait>(
      `api/resources/portrait?${parameters}`,
      token,
      signal
    );
  },

  getResourcePage(
    resourceId: string,
    token: string,
    signal?: AbortSignal
  ): Promise<SemanticResourcePage> {
    const parameters = query({ id: resourceId, lang: "ru" });
    return requestJson<SemanticResourcePage>(
      `api/resources/page?${parameters}`,
      token,
      signal
    );
  },

  getDocumentLocation(
    uri: string,
    token: string,
    signal?: AbortSignal
  ): Promise<DocumentLocation> {
    return requestJson<DocumentLocation>(
      `api/documents/location?${query({ uri })}`,
      token,
      signal
    );
  },

  getDocumentBlob(
    uri: string,
    variant: DocumentVariant,
    token: string,
    signal?: AbortSignal
  ): Promise<Blob> {
    return requestBlob(
      documentContentUrl(uri, variant),
      token,
      signal
    );
  }
};
