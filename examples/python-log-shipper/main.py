import os
import sys

from sentinel_sdk import LogEntryInput, LoginRequest, SentinelClient

BASE_URL = os.getenv("SENTINEL_BASE_URL", "http://localhost:5000")
SERVICE = os.getenv("SENTINEL_SERVICE", "python-log-shipper")
ENVIRONMENT = os.getenv("SENTINEL_ENVIRONMENT", "development")


def main() -> int:
    client = SentinelClient(
        base_url=BASE_URL,
        access_token=os.getenv("SENTINEL_ACCESS_TOKEN"),
        tenant_id=os.getenv("SENTINEL_TENANT_ID"),
    )

    if not client.access_token:
        email = os.getenv("SENTINEL_EMAIL")
        password = os.getenv("SENTINEL_PASSWORD")
        if not email or not password:
            print("Set SENTINEL_ACCESS_TOKEN or SENTINEL_EMAIL and SENTINEL_PASSWORD.", file=sys.stderr)
            return 1

        client.login(LoginRequest(email=email, password=password, tenant_id=os.getenv("SENTINEL_TENANT_ID")))

    response = client.ingest_logs(
        [
            LogEntryInput(
                service=SERVICE,
                environment=ENVIRONMENT,
                level="info",
                message="python-log-shipper started",
                attributes={"host": os.getenv("HOSTNAME", "localhost")},
            ),
            LogEntryInput(
                service=SERVICE,
                environment=ENVIRONMENT,
                level="warning",
                message="sample warning event shipped to Sentinel",
                attributes={"sample": "true"},
            ),
        ]
    )

    print(f"Shipped {response.accepted_count} log entries ({response.status}).")
    client.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
