import type { SemanticContentItem } from "./model";

export {};

declare global {
  interface Array<T> {
    /**
     * TypeScript 5.9 does not infer the union element type when this semantic
     * table projection returns either rows with a member or a row with
     * member: null. Runtime Array.flatMap supports both branches normally.
     * Keep the additional overload restricted to SemanticContentItem arrays.
     */
    flatMap<TResult extends readonly unknown[]>(
      this: T extends SemanticContentItem ? T[] : never,
      callbackfn: (
        this: undefined,
        value: T,
        index: number,
        array: T[]
      ) => TResult,
      thisArg?: unknown
    ): Array<TResult[number]>;
  }
}
