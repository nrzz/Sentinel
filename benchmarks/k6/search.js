import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const searchDuration = new Trend('search_duration', true);

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';
const TENANT_ID = __ENV.TENANT_ID || '00000000-0000-0000-0000-000000000001';

export const options = {
  scenarios: {
    search_load: {
      executor: 'constant-vus',
      vus: 30,
      duration: '5m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<800', 'p(99)<2000'],
    http_req_failed: ['rate<0.02'],
    errors: ['rate<0.02'],
    search_duration: ['p(95)<600'],
  },
};

const queries = [
  'error',
  'level:error',
  'service:api',
  'environment:production',
  'timeout',
  'exception',
  'failed',
  'warning',
];

export default function () {
  const query = queries[__ITER % queries.length];
  const from = new Date(Date.now() - 3600000).toISOString();
  const to = new Date().toISOString();

  const url = `${BASE_URL}/api/v1/search/logs?query=${encodeURIComponent(query)}&from=${from}&to=${to}&limit=100&offset=0`;

  const params = {
    headers: {
      Authorization: `Bearer ${AUTH_TOKEN}`,
      'X-Tenant-Id': TENANT_ID,
    },
    tags: { name: 'search_logs' },
  };

  const start = Date.now();
  const res = http.get(url, params);
  searchDuration.add(Date.now() - start);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response has items': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body.items);
      } catch {
        return false;
      }
    },
  });

  errorRate.add(!success);
  sleep(0.5);
}

export function handleSummary(data) {
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}
