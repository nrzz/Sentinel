"""Official Python client SDK for the Sentinel observability platform."""

from sentinel_sdk.client import SentinelApiError, SentinelClient
from sentinel_sdk.models import (
    AlertRule,
    AuthResponse,
    Incident,
    IngestLogsResponse,
    IngestMetricsResponse,
    LogEntryInput,
    LoginRequest,
    LogSearchItem,
    LogSearchQuery,
    MetricEntryInput,
    MetricItem,
    MetricQuery,
    QueryMetricsResponse,
    SearchLogsResponse,
    Tenant,
)

__all__ = [
    "AlertRule",
    "AuthResponse",
    "Incident",
    "IngestLogsResponse",
    "IngestMetricsResponse",
    "LogEntryInput",
    "LogSearchItem",
    "LogSearchQuery",
    "LoginRequest",
    "MetricEntryInput",
    "MetricItem",
    "MetricQuery",
    "QueryMetricsResponse",
    "SearchLogsResponse",
    "SentinelApiError",
    "SentinelClient",
    "Tenant",
]

__version__ = "1.0.0"
