# OWASP ASVS 4.0 Checklist — Sentinel

Reference: [OWASP Application Security Verification Standard](https://owasp.org/www-project-application-security-verification-standard/)

Status legend: ✅ Implemented | ⚠️ Partial | ❌ Not implemented | N/A Not applicable

## V1 Architecture, Design and Threat Modeling

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 1.1.1 | Secure SDLC with security user stories | ⚠️ | Threat model in `security/threat-model.md` |
| 1.1.2 | Threat modeling for high-risk components | ✅ | STRIDE analysis documented |
| 1.4.1 | Components segregated by trust boundary | ✅ | `sentinel-system` / `sentinel-platform` namespaces |
| 1.14.1 | Dependency vulnerability scanning | ✅ | NuGet audit in CI; NU1902 suppressed for OTel |

## V2 Authentication

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 2.1.1 | Strong password policy | ⚠️ | FluentValidation on registration; enforce complexity in UI |
| 2.1.7 | Passwords hashed with approved algorithm | ✅ | BCrypt work factor 12 (`PasswordHasher`) |
| 2.2.1 | Anti-automation on login | ❌ | Add rate limiting at ingress |
| 2.2.2 | Weak password check | ❌ | Future: zxcvbn integration |
| 2.3.1 | Session tokens use secure random | ✅ | `RandomNumberGenerator` for refresh tokens |
| 2.5.1 | Initial passwords not hardcoded | ✅ | Seeder uses env-specific config |

## V3 Session Management

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 3.2.1 | Session tokens on logout invalidated | ⚠️ | Refresh token revocation in DB |
| 3.5.1 | Token-based sessions use cryptographic signing | ✅ | HMAC-SHA256 JWT |
| 3.5.2 | Token expiration enforced | ✅ | `ValidateLifetime = true` |
| 3.7.1 | Defenses against session fixation | ✅ | New tokens issued on login |

## V4 Access Control

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 4.1.1 | Access control enforced on trusted layer | ✅ | `RbacService`, `[RequireAuthorization]` |
| 4.1.2 | Attribute-based access for tenants | ✅ | `tenant_id` claim + `TenantResolver` |
| 4.1.3 | Principle of least privilege | ⚠️ | Default roles seeded; review per deployment |
| 4.2.1 | IDOR prevention | ✅ | Tenant-scoped repository queries |

## V5 Validation, Sanitization and Encoding

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 5.1.1 | Input validation on all parameters | ✅ | FluentValidation on request DTOs |
| 5.2.1 | Sanitize untrusted HTML | N/A | API-only; web uses React escaping |
| 5.3.1 | Output encoding | ✅ | JSON serialization via ASP.NET Core |

## V6 Stored Cryptography

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 6.2.1 | Approved crypto for data at rest | ⚠️ | Operator enables disk encryption on PVCs |
| 6.2.2 | Approved crypto for data in transit | ✅ | TLS ingress; `ssl-redirect` annotation |
| 6.2.3 | IVs not reused | N/A | No custom symmetric encryption |

## V7 Error Handling and Logging

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 7.1.1 | No sensitive data in logs | ✅ | `SecretRedactor` for AI; structured logging |
| 7.1.2 | Error messages generic to users | ✅ | `GlobalExceptionHandler` |
| 7.4.1 | Security events logged | ⚠️ | `AuditService` for identity events |

## V8 Data Protection

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 8.2.1 | Sensitive data identified | ✅ | Threat model asset classification |
| 8.3.1 | Sensitive data not cached | ⚠️ | Redis used for non-sensitive caching |
| 8.3.4 | Sensitive data not in URLs | ✅ | JWT in Authorization header |

## V9 Communication

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 9.1.1 | TLS for all connections | ✅ | Ingress TLS; internal cluster TLS optional |
| 9.1.2 | TLS 1.2+ only | ⚠️ | Configure at ingress controller |
| 9.2.1 | Server certificate validated | ✅ | cert-manager integration |

## V10 Malicious Code

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 10.2.1 | Code integrity via CI | ✅ | GitHub Actions build pipeline |
| 10.3.1 | No unauthorized code in production | ✅ | Container images from CI registry |

## V11 Business Logic

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 11.1.1 | Business logic flows in correct order | ✅ | Auth before protected endpoints |
| 11.1.4 | Anti-automation on expensive operations | ❌ | Add rate limits on search/AI |

## V12 Files and Resources

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 12.1.1 | File upload restrictions | N/A | No file upload endpoints |

## V13 API and Web Service

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 13.1.1 | REST URL patterns consistent | ✅ | `/api/v1/*` versioning |
| 13.2.1 | RESTful HTTP methods correct | ✅ | GET/POST/PUT/DELETE semantics |
| 13.2.3 | JSON schema validation | ✅ | FluentValidation |
| 13.4.1 | GraphQL/gRPC authorization | ✅ | gRPC behind same auth middleware |

## V14 Configuration

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| 14.1.1 | Secure build and deploy process | ✅ | Helm + Argo CD GitOps |
| 14.2.1 | Dependency versions pinned | ✅ | `Directory.Build.props`, lock files |
| 14.3.1 | Secrets not in source control | ✅ | Kubernetes secrets; `.env` gitignored |

## Pre-Production Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Engineering Lead | | | |
| Security Reviewer | | | |
| Operations Lead | | | |
