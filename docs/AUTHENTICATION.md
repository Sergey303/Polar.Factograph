# Authentication

Production authentication uses JWT bearer tokens issued by an OpenID Connect authority.

## Configuration

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://identity.example.org",
      "Audience": "polar-factograph",
      "RequireHttpsMetadata": true
    }
  }
}
```

`Authority` and `Audience` must be configured together. The authority must be an absolute HTTPS URI while `RequireHttpsMetadata` is enabled.

For a local identity provider over HTTP, disable metadata HTTPS only in a development-specific configuration:

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "http://localhost:8080",
      "Audience": "polar-factograph",
      "RequireHttpsMetadata": false
    }
  }
}
```

## Identity mapping

JWT claim mapping is disabled so the original `sub` claim is preserved. The application resolves the project user id from:

1. `ClaimTypes.NameIdentifier`;
2. `sub`;
3. `Identity.Name`.

The API never accepts a user id from query parameters or custom headers.

## Development fallback

`Api:DevelopmentUserId` is used only when the host environment is `Development` and no authenticated identity exists. It is ignored in production.

When JWT settings are absent, the host still starts, but protected project routes return `401 authentication_required` unless the development fallback applies.
