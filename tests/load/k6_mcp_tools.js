// k6 load test for DINOForge MCP REST API
// Run: k6 run tests/load/k6_mcp_tools.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const toolLatency = new Trend('tool_latency');

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // ramp up
    { duration: '1m', target: 10 },     // steady state
    { duration: '30s', target: 20 },    // spike
    { duration: '1m', target: 20 },     // sustained spike
    { duration: '30s', target: 0 },     // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    errors: ['rate<0.1'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export default function () {
  // Health check
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
    'health response has status field': (r) => JSON.parse(r.body).status === 'healthy',
  });
  errorRate.add(healthRes.status !== 200);

  // List tools
  const toolsRes = http.get(`${BASE_URL}/tools`);
  check(toolsRes, {
    'tools status is 200': (r) => r.status === 200,
    'tools returns array': (r) => Array.isArray(JSON.parse(r.body)),
  });
  errorRate.add(toolsRes.status !== 200);

  sleep(1);
}

export function handleSummary(data) {
  return {
    'tests/load/k6_summary.json': JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}
