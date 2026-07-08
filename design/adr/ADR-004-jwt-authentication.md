# ADR-004: JWT Authentication with In-Memory Token Storage

## Status

Accepted

## Context

Sentinel requires authentication for its API and real-time features (SignalR). Requirements:

- Stateless API that can scale horizontally
- Short-lived access tokens with refresh capability
- Secure token storage on the frontend (XSS mitigation)
- SignalR hub authentication via bearer tokens

## Decision

- **Backend:** Issue JWT access tokens (60 min) and opaque refresh tokens (7 days). Refresh tokens stored in PostgreSQL. Validate JWTs via `Microsoft.AspNetCore.Authentication.JwtBearer`.
- **Frontend:** Store tokens in **Zustand in-memory state only** (not localStorage or sessionStorage). On page refresh, user must re-authenticate unless we add a httpOnly cookie refresh flow later.

Token flow:
```
Login → accessToken (memory) + refreshToken (memory)
API call → Authorization: Bearer {accessToken}
401 response → POST /api/v1/auth/refresh → new tokens
Token expired + refresh fails → redirect to /login
```

SignalR passes the access token via `accessTokenFactory` in the hub connection builder.

## Consequences

**Positive:**
- Tokens not persisted in browser storage — reduces XSS attack surface
- Stateless API validation — no server-side session store for access tokens
- Refresh tokens revocable in PostgreSQL
- Standard JWT ecosystem (libraries, tooling, debugging)

**Negative:**
- Page refresh loses authentication (user must log in again)
- No "remember me" without adding httpOnly cookie support
- Token refresh race conditions possible with concurrent API calls (mitigated by refresh queue in API client)

## Alternatives Considered

- **localStorage tokens** — Rejected due to XSS vulnerability.
- **httpOnly cookie sessions** — Planned for v0.2 to support persistent sessions.
- **OAuth2/OIDC only** — Planned for v0.4 SSO integration; JWT remains the internal token format.
