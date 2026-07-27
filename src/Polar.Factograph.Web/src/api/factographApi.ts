import { requestBlob, requestJson } from "./http";
import type {
  DocumentLocation,
  DocumentVariant,
  ProjectOverview,
  ResourcePortrait,
  ResourceSearchResult
} from "./models";
import { mergeSearchResults } from "./searchResults";

function query(parameters: Record<string, string | number>): string {
  const values = new URLSearchParams();
  for (const [name, value] of Object.entries(parameters)) {
    values.set(name, String(value));
  }
  return values.toString();
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
      `api/documents/content?${query({ uri, variant })}`,
      token,
      signal
    );
  }
};
