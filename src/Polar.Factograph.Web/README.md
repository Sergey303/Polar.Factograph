# Polar.Factograph Web

React/TypeScript client for the project-wide Factograph workspace.

## Run locally

Start the API first, then run:

```bash
cd src/Polar.Factograph.Web
npm install
npm run dev
```

Vite proxies `/api` to `http://localhost:5000` by default. Override the target without changing source code:

```bash
FACTOGRAPH_API_URL=https://localhost:7001 npm run dev
```

## Authentication

Development API configuration may resolve its configured development member without a token. For production-like testing, open **Доступ** in the top bar and paste a JWT. The token is kept only in `sessionStorage`; it is not written to local storage or a cookie.

## Implemented vertical slice

- load project identity, current member and readable cassettes;
- show the effective default write cassette or read-only state;
- explicit name-prefix and normalized-word search modes;
- deterministic result list with type and match evidence;
- ontology-labelled resource portrait;
- direct and inverse relation navigation;
- authorized preview and original document loading.

The client never treats a cassette id supplied by the browser as authority. Read scope and write routing continue to be resolved by the API.
