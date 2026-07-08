from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class LoginRequest:
    email: str
    password: str
    tenant_id: str | None = None


@dataclass(slots=True)
class AuthUser:
    id: str
    email: str
    display_name: str
    tenant_id: str
    roles: list[str]
    permissions: list[str]


@dataclass(slots=True)
class AuthResponse:
    access_token: str
    refresh_token: str
    access_token_expires_at: str
    user: AuthUser


@dataclass(slots=True)
class LogEntryInput:
    service: str
    environment: str
    level: str
    message: str
    attributes: dict[str, str] | None = None
    trace_id: str | None = None
    span_id: str | None = None
    correlation_id: str | None = None
    timestamp: str | None = None


@dataclass(slots=True)
class IngestLogsResponse:
    accepted_count: int
    status: str


@dataclass(slots=True)
class LogSearchQuery:
    from_: str | None = None
    to: str | None = None
    level: str | None = None
    service: str | None = None
    environment: str | None = None
    query: str | None = None
    trace_id: str | None = None
    limit: int = 100
    offset: int = 0


@dataclass(slots=True)
class LogSearchItem:
    id: str
    timestamp: str
    service: str
    environment: str
    level: str
    normalized_level: str
    message: str
    trace_id: str | None = None
    span_id: str | None = None
    correlation_id: str | None = None
    parsed_exception: str | None = None
    source_host: str | None = None


@dataclass(slots=True)
class SearchLogsResponse:
    items: list[LogSearchItem]
    total_count: int
    limit: int
    offset: int


@dataclass(slots=True)
class MetricEntryInput:
    name: str
    value: float
    unit: str
    service: str
    environment: str
    tags: dict[str, str] | None = None
    timestamp: str | None = None


@dataclass(slots=True)
class IngestMetricsResponse:
    accepted_count: int
    status: str


@dataclass(slots=True)
class MetricQuery:
    from_: str | None = None
    to: str | None = None
    name: str | None = None
    service: str | None = None
    environment: str | None = None
    limit: int = 1000


@dataclass(slots=True)
class MetricItem:
    id: str
    timestamp: str
    name: str
    value: float
    unit: str
    service: str
    environment: str
    tags: dict[str, str] = field(default_factory=dict)


@dataclass(slots=True)
class QueryMetricsResponse:
    items: list[MetricItem]


@dataclass(slots=True)
class AlertRule:
    id: str
    tenant_id: str
    name: str
    description: str
    query: str
    condition: str
    severity: str
    status: str
    evaluation_interval_seconds: int
    notification_channels: list[str]
    created_by: str | None
    created_at: str
    updated_at: str


@dataclass(slots=True)
class Incident:
    id: str
    title: str
    description: str
    severity: str
    status: str
    assigned_to: str | None
    source_alert_execution_id: str | None
    created_by: str | None
    resolved_at: str | None
    created_at: str
    updated_at: str


@dataclass(slots=True)
class Tenant:
    id: str
    name: str
    slug: str
    is_active: bool
    environment: str
    settings_json: str
    created_at: str
    updated_at: str
