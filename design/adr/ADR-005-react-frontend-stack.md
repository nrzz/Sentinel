# ADR-005: React 19 with Vite and Tailwind CSS

## Status

Accepted

## Context

Sentinel needs a modern, responsive web UI for observability data visualization. Requirements:

- Real-time updates (SignalR for live logs)
- Large dataset rendering (virtualized tables for logs)
- Chart visualization (metrics time series)
- Dark mode as default (observability tools are used in low-light environments)
- Fast development iteration with hot module replacement

## Decision

Build the frontend with:

| Technology | Purpose |
|---|---|
| React 19 | UI framework |
| Vite 6 | Build tool and dev server |
| TypeScript | Type safety |
| Tailwind CSS 4 | Utility-first styling |
| React Router 7 | Client-side routing |
| TanStack Query | Server state management |
| TanStack Virtual | Virtualized log table |
| Zustand | Client state (auth) |
| Recharts | Metrics charts |
| @microsoft/signalr | Real-time log streaming |

The app lives at `src/sentinel-web/` and is served in production via nginx (Dockerfile.web).

## Consequences

**Positive:**
- Fast dev experience with Vite HMR
- Tailwind enables rapid UI development with consistent dark theme
- TanStack Query handles caching, refetching, and loading states
- Virtual scrolling handles large log datasets without DOM bloat
- Zustand is minimal and sufficient for auth state

**Negative:**
- No component library (building UI from scratch with Tailwind)
- Recharts less feature-rich than Grafana's charting (sufficient for v0.1)
- Client-side rendering only (no SSR — acceptable for internal tool)

## Alternatives Considered

- **Next.js** — SSR not needed for an authenticated dashboard app; adds complexity.
- **Blazor WebAssembly** — Would unify language with backend but smaller ecosystem for charting/virtualization.
- **Grafana plugin** — Would limit UI customization; Sentinel needs its own identity.
