import { SentinelClient } from '@sentinel/sdk';

const baseUrl = process.env.SENTINEL_BASE_URL ?? 'http://localhost:5000';
const service = process.env.SENTINEL_SERVICE ?? 'node-log-shipper';
const environment = process.env.SENTINEL_ENVIRONMENT ?? 'development';

const client = new SentinelClient({
  baseUrl,
  accessToken: process.env.SENTINEL_ACCESS_TOKEN,
  tenantId: process.env.SENTINEL_TENANT_ID,
});

if (!process.env.SENTINEL_ACCESS_TOKEN) {
  const email = process.env.SENTINEL_EMAIL;
  const password = process.env.SENTINEL_PASSWORD;
  if (!email || !password) {
    throw new Error('Set SENTINEL_ACCESS_TOKEN or SENTINEL_EMAIL and SENTINEL_PASSWORD.');
  }

  await client.login({
    email,
    password,
    tenantId: process.env.SENTINEL_TENANT_ID,
  });
}

const response = await client.ingestLogs([
  {
    service,
    environment,
    level: 'info',
    message: 'node-log-shipper started',
    attributes: { host: process.env.HOSTNAME ?? 'localhost' },
  },
  {
    service,
    environment,
    level: 'error',
    message: 'sample error event shipped to Sentinel',
    attributes: { sample: 'true' },
  },
]);

console.log(`Shipped ${response.acceptedCount} log entries (${response.status}).`);
