# Authentication

Production authentication uses JWT bearer tokens issued by an OpenID Connect authority. The React workspace uses Authorization Code with PKCE and never receives a client secret.

## Configuration

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://identity.example.org",
      "Audience": "polar-factograph",
      "RequireHttpsMetadata": true
    },
    "Browser": {
      "ClientId": "polar-factograph-web",
      "Scope": "openid profile polar-factograph"
    }
  }
}
```

`Authority` and `Audience` must be configured together. The authority must be an absolute HTTPS URI while `RequireHttpsMetadata` is enabled.

`Authentication:Browser` is optional. `ClientId` and `Scope` must either both be absent or both be configured, and browser login requires the JWT section because the API must validate the resulting access token. The client id must identify a public client with Authorization Code and PKCE enabled; no client secret belongs in this repository or browser configuration.

For a local identity provider over HTTP, disable metadata HTTPS only in a development-specific configuration:

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "http://localhost:8080",
      "Audience": "polar-factograph",
      "RequireHttpsMetadata": false
    },
    "Browser": {
      "ClientId": "polar-factograph-web",
      "Scope": "openid profile polar-factograph"
    }
  }
}
```

## Browser client registration

Register the exact React origin and path as an allowed redirect URI. The default development callback is:

```text
http://localhost:5173/
```

The provider must allow that browser origin to call its token endpoint. The workspace loads the provider discovery document, verifies that its `issuer` matches the configured authority, and exchanges the code directly with `code_verifier`; only bearer tokens with a positive `expires_in` are accepted.

The API exposes the non-secret public settings through:

```text
GET /api/auth/browser
```

The response contains only `enabled`, `authority`, `clientId`, and `scope`. It never contains a client secret, signing key, members, roles, or project configuration.

## Session behavior

The access token, its source, expiry time, PKCE verifier, and state exist only in `sessionStorage`. They are removed when the tab closes. Authorization callback parameters are removed from the address bar before the code exchange continues.

The PKCE request state is valid for ten minutes. A mismatched state, redirect URI, expired request, unexpected discovery issuer, unsupported token type, or incomplete token response aborts the login.

When `expires_in` is reached, the workspace clears the local token and project state. **Выйти** ends the Polar.Factograph browser session; the identity provider may retain its own single-sign-on session, so a later login can complete without asking for credentials again.

## Identity mapping

JWT claim mapping is disabled so the original `sub` claim is preserved. The application resolves the project user id from:

1. `ClaimTypes.NameIdentifier`;
2. `sub`;
3. `Identity.Name`.

The API never accepts a user id from query parameters or custom headers.

## Development and diagnostics

`Api:DevelopmentUserId` is used only when the host environment is `Development` and no authenticated identity exists. It is ignored in production.

When JWT settings are absent, the host still starts, but protected project routes return `401 authentication_required` unless the development fallback applies.

The React **Диагностика** menu can still accept a bearer token for provider testing. This is not the normal production login path. The diagnostic token is also stored only in `sessionStorage` and is never written to a cookie or local storage.
