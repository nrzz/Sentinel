from __future__ import annotations

import json
import time
from typing import Any, TypeVar

import httpx

from sentinel_sdk.models import (
    AlertRule,
    AuthResponse,
    AuthUser,
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

T = TypeVar("T")

DEFAULT_BASE_URL = "http://localhost:5000"
DEFAULT_TENANT_HEADER = "X-Tenant-ID"
DEFAULT_TIMEOUT = 30.0
DEFAULT_MAX_RETRIES = 3


class SentinelApiError(Exception):
    def __init__(self, status_code: int, message: str, response_body: str | None = None) -> None:
        super().__init__(f"Sentinel API error {status_code}: {message}")
        self.status_code = status_code
        self.response_body = response_body


class SentinelClient:
    def __init__(
        self,
        base_url: str = DEFAULT_BASE_URL,
        access_token: str | None = None,
        refresh_token: str | None = None,
        tenant_id: str | None = None,
        tenant_header_name: str = DEFAULT_TENANT_HEADER,
        timeout: float = DEFAULT_TIMEOUT,
        max_retry_attempts: int = DEFAULT_MAX_RETRIES,
        client: httpx.Client | None = None,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.access_token = access_token
        self.refresh_token = refresh_token
        self.tenant_id = tenant_id
        self.tenant_header_name = tenant_header_name
        self.timeout = timeout
        self.max_retry_attempts = max_retry_attempts
        self._client = client or httpx.Client(timeout=timeout)

    def set_access_token(self, access_token: str, tenant_id: str | None = None) -> None:
        self.access_token = access_token
        if tenant_id is not None:
            self.tenant_id = tenant_id

    def login(self, request: LoginRequest) -> AuthResponse:
        payload: dict[str, Any] = {
            "email": request.email,
            "password": request.password,
        }
        if request.tenant_id is not None:
            payload["tenantId"] = request.tenant_id

        response = self._post("/api/v1/auth/login", payload, authenticated=False)
        auth = _parse_auth_response(response)
        self.access_token = auth.access_token
        self.refresh_token = auth.refresh_token
        self.tenant_id = auth.user.tenant_id
        return auth

    def refresh_token_exchange(self) -> AuthResponse:
        if not self.refresh_token:
            raise ValueError("Refresh token is not configured.")

        response = self._post(
            "/api/v1/auth/refresh",
            {"refreshToken": self.refresh_token},
            authenticated=False,
        )
        auth = _parse_auth_response(response)
        self.access_token = auth.access_token
        self.refresh_token = auth.refresh_token
        self.tenant_id = auth.user.tenant_id
        return auth

    def ingest_logs(self, logs: list[LogEntryInput]) -> IngestLogsResponse:
        payload = {
            "logs": [
                {
                    "service": log.service,
                    "environment": log.environment,
                    "level": log.level,
                    "message": log.message,
                    "attributes": log.attributes,
                    "traceId": log.trace_id,
                    "spanId": log.span_id,
                    "correlationId": log.correlation_id,
                    "timestamp": log.timestamp,
                }
                for log in logs
            ]
        }
        response = self._post("/api/v1/logs", payload, authenticated=True)
        return IngestLogsResponse(
            accepted_count=response["acceptedCount"],
            status=response["status"],
        )

    def search_logs(self, query: LogSearchQuery | None = None) -> SearchLogsResponse:
        query = query or LogSearchQuery()
        params = {
            "from": query.from_,
            "to": query.to,
            "level": query.level,
            "service": query.service,
            "environment": query.environment,
            "query": query.query,
            "traceId": query.trace_id,
            "limit": query.limit,
            "offset": query.offset,
        }
        response = self._get("/api/v1/search/logs", params=params, authenticated=True)
        items = [
            LogSearchItem(
                id=item["id"],
                timestamp=item["timestamp"],
                service=item["service"],
                environment=item["environment"],
                level=item["level"],
                normalized_level=item["normalizedLevel"],
                message=item["message"],
                trace_id=item.get("traceId"),
                span_id=item.get("spanId"),
                correlation_id=item.get("correlationId"),
                parsed_exception=item.get("parsedException"),
                source_host=item.get("sourceHost"),
            )
            for item in response["items"]
        ]
        return SearchLogsResponse(
            items=items,
            total_count=response["totalCount"],
            limit=response["limit"],
            offset=response["offset"],
        )

    def ingest_metrics(self, metrics: list[MetricEntryInput]) -> IngestMetricsResponse:
        payload = {
            "metrics": [
                {
                    "name": metric.name,
                    "value": metric.value,
                    "unit": metric.unit,
                    "tags": metric.tags,
                    "service": metric.service,
                    "environment": metric.environment,
                    "timestamp": metric.timestamp,
                }
                for metric in metrics
            ]
        }
        response = self._post("/api/v1/metrics", payload, authenticated=True)
        return IngestMetricsResponse(
            accepted_count=response["acceptedCount"],
            status=response["status"],
        )

    def query_metrics(self, query: MetricQuery | None = None) -> QueryMetricsResponse:
        query = query or MetricQuery()
        params = {
            "from": query.from_,
            "to": query.to,
            "name": query.name,
            "service": query.service,
            "environment": query.environment,
            "limit": query.limit,
        }
        response = self._get("/api/v1/metrics", params=params, authenticated=True)
        items = [
            MetricItem(
                id=item["id"],
                timestamp=item["timestamp"],
                name=item["name"],
                value=item["value"],
                unit=item["unit"],
                service=item["service"],
                environment=item["environment"],
                tags=item.get("tags", {}),
            )
            for item in response["items"]
        ]
        return QueryMetricsResponse(items=items)

    def list_alerts(self) -> list[AlertRule]:
        response = self._get("/api/v1/alerts", authenticated=True)
        return [_parse_alert_rule(item) for item in response]

    def get_alert(self, alert_id: str) -> AlertRule:
        response = self._get(f"/api/v1/alerts/{alert_id}", authenticated=True)
        return _parse_alert_rule(response)

    def list_incidents(self) -> list[Incident]:
        response = self._get("/api/v1/incidents", authenticated=True)
        return [_parse_incident(item) for item in response]

    def get_incident(self, incident_id: str) -> Incident:
        response = self._get(f"/api/v1/incidents/{incident_id}", authenticated=True)
        return _parse_incident(response)

    def list_tenants(self) -> list[Tenant]:
        response = self._get("/api/v1/tenants", authenticated=True)
        return [_parse_tenant(item) for item in response]

    def get_tenant(self, tenant_id: str) -> Tenant:
        response = self._get(f"/api/v1/tenants/{tenant_id}", authenticated=True)
        return _parse_tenant(response)

    def close(self) -> None:
        self._client.close()

    def _get(
        self,
        path: str,
        params: dict[str, Any] | None = None,
        authenticated: bool = True,
    ) -> Any:
        return self._request("GET", path, params=params, authenticated=authenticated)

    def _post(self, path: str, payload: dict[str, Any], authenticated: bool) -> Any:
        return self._request("POST", path, json_body=payload, authenticated=authenticated)

    def _request(
        self,
        method: str,
        path: str,
        params: dict[str, Any] | None = None,
        json_body: dict[str, Any] | None = None,
        authenticated: bool = True,
    ) -> Any:
        headers = {"Accept": "application/json"}
        if json_body is not None:
            headers["Content-Type"] = "application/json"
        if authenticated and self.access_token:
            headers["Authorization"] = f"Bearer {self.access_token}"
        if authenticated and self.tenant_id:
            headers[self.tenant_header_name] = self.tenant_id

        filtered_params = {
            key: value
            for key, value in (params or {}).items()
            if value is not None and value != ""
        }

        last_error: Exception | None = None
        for attempt in range(self.max_retry_attempts + 1):
            try:
                response = self._client.request(
                    method,
                    f"{self.base_url}{path}",
                    params=filtered_params,
                    json=json_body,
                    headers=headers,
                )
                body = response.text
                if response.is_success:
                    return response.json() if body else None

                message = _parse_error_message(body) or response.reason_phrase
                if response.status_code == 429 or response.status_code >= 500:
                    last_error = SentinelApiError(response.status_code, message or "Request failed", body)
                    time.sleep((2 ** (attempt + 1)) * 0.1)
                    continue

                raise SentinelApiError(response.status_code, message or "Request failed", body)
            except httpx.RequestError as exc:
                last_error = exc
                time.sleep((2 ** (attempt + 1)) * 0.1)

        if last_error is not None:
            raise last_error
        raise RuntimeError("Request failed")


def _parse_error_message(body: str) -> str | None:
    try:
        payload = json.loads(body)
    except json.JSONDecodeError:
        return None
    return payload.get("error") or payload.get("title")


def _parse_auth_response(payload: dict[str, Any]) -> AuthResponse:
    user = payload["user"]
    return AuthResponse(
        access_token=payload["accessToken"],
        refresh_token=payload["refreshToken"],
        access_token_expires_at=payload["accessTokenExpiresAt"],
        user=AuthUser(
            id=user["id"],
            email=user["email"],
            display_name=user["displayName"],
            tenant_id=user["tenantId"],
            roles=user.get("roles", []),
            permissions=user.get("permissions", []),
        ),
    )


def _parse_alert_rule(payload: dict[str, Any]) -> AlertRule:
    return AlertRule(
        id=payload["id"],
        tenant_id=payload["tenantId"],
        name=payload["name"],
        description=payload["description"],
        query=payload["query"],
        condition=payload["condition"],
        severity=payload["severity"],
        status=payload["status"],
        evaluation_interval_seconds=payload["evaluationIntervalSeconds"],
        notification_channels=payload.get("notificationChannels", []),
        created_by=payload.get("createdBy"),
        created_at=payload["createdAt"],
        updated_at=payload["updatedAt"],
    )


def _parse_incident(payload: dict[str, Any]) -> Incident:
    return Incident(
        id=payload["id"],
        title=payload["title"],
        description=payload["description"],
        severity=payload["severity"],
        status=payload["status"],
        assigned_to=payload.get("assignedTo"),
        source_alert_execution_id=payload.get("sourceAlertExecutionId"),
        created_by=payload.get("createdBy"),
        resolved_at=payload.get("resolvedAt"),
        created_at=payload["createdAt"],
        updated_at=payload["updatedAt"],
    )


def _parse_tenant(payload: dict[str, Any]) -> Tenant:
    return Tenant(
        id=payload["id"],
        name=payload["name"],
        slug=payload["slug"],
        is_active=payload["isActive"],
        environment=payload["environment"],
        settings_json=payload["settingsJson"],
        created_at=payload["createdAt"],
        updated_at=payload["updatedAt"],
    )
