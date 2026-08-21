import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';
const errorRate = new Rate('errors');
const toolLatency = new Trend('tool_latency');
const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 100 },
    { duration: '2m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: { http_req_duration: ['p(95)<2000'], errors: ['rate<0.1'] },
};
export default function () {
  const endpoints = [{ method: 'GET', url: '/health' }, { method: 'GET', url: '/tools' }, { method: 'GET', url: '/metrics' }];
  const ep = endpoints[Math.floor(Math.random() * endpoints.length)];
  const start = Date.now();
  const res = http.request(ep.method, BASE_URL + ep.url);
  toolLatency.add(Date.now() - start);
  errorRate.add(res.status >= 400);
  check(res, { 'status is 200': (r) => r.status === 200, 'response time < 2s': (r) => r.timings.duration < 2000 });
  sleep(Math.random() * 2 + 0.5);
}
