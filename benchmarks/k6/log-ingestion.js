import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const ingestDuration = new Trend('ingest_duration', true);
const logsIngested = new Counter('logs_ingested');

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TENANT_ID = __ENV.TENANT_ID || '00000000-0000-0000-0000-000000000001';
const BATCH_SIZE = parseInt(__ENV.BATCH_SIZE || '50', 10);

export const options = {
  scenarios: {
    ramp_up: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '3m', target: 50 },
        { duration: '2m', target: 100 },
        { duration: '1m', target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
    ingest_duration: ['p(95)<400'],
  },
};

function buildPayload(batchSize) {
  const logs = [];
  const now = new Date().toISOString();
  for (let i = 0; i < batchSize; i++) {
    logs.push({
      timestamp: now,
      service: `load-test-service-${__VU}`,
      environment: 'loadtest',
      level: ['info', 'warn', 'error'][i % 3],
      message: `Load test log entry ${__ITER}-${i} from VU ${__VU}`,
      attributes: {
        vu: String(__VU),
        iter: String(__ITER),
        index: String(i),
      },
      traceId: `trace-${__VU}-${__ITER}`,
      correlationId: `corr-${__VU}-${__ITER}`,
    });
  }
  return JSON.stringify({ logs });
}

export default function () {
  const payload = buildPayload(BATCH_SIZE);
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant-Id': TENANT_ID,
    },
    tags: { name: 'ingest_logs' },
  };

  const start = Date.now();
  const res = http.post(`${BASE_URL}/api/v1/logs`, payload, params);
  ingestDuration.add(Date.now() - start);

  const success = check(res, {
    'status is 202': (r) => r.status === 202,
    'response has accepted count': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.accepted >= 1;
      } catch {
        return false;
      }
    },
  });

  errorRate.add(!success);
  if (success) {
    logsIngested.add(BATCH_SIZE);
  }

  sleep(0.1);
}

export function handleSummary(data) {
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}
