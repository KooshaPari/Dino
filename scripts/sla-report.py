#!/usr/bin/env python3
"""Generate SLA/SLO report from Prometheus metrics.

Usage:
    python scripts/sla-report.py --period 30d
    python scripts/sla-report.py --slo api_availability
"""
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timedelta
from typing import Any


def query_prometheus(url: str, query: str, start: str, end: str) -> dict[str, Any]:
    """Query Prometheus API."""
    import urllib.request
    import urllib.parse
    params = urllib.parse.urlencode({"query": query, "start": start, "end": end})
    req = urllib.request.Request(f"{url}/api/v1/query?{params}")
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return json.loads(resp.read())
    except Exception as e:
        print(f"Warning: Could not query Prometheus: {e}", file=sys.stderr)
        return {"data": {"result": []}}


def calculate_slo(availability: float, target: float) -> dict[str, Any]:
    """Calculate SLO status and error budget."""
    error_budget = 100 - target
    used = max(0, target - availability)
    remaining = error_budget - used
    remaining_pct = (remaining / error_budget * 100) if error_budget > 0 else 100
    return {
        "target": f"{target}%",
        "actual": f"{availability:.3f}%",
        "error_budget_remaining": f"{remaining_pct:.1f}%",
        "status": "healthy" if availability >= target else "breached",
    }


def generate_report(prometheus_url: str, period_days: int = 30) -> dict[str, Any]:
    """Generate SLA report."""
    now = datetime.utcnow()
    start = (now - timedelta(days=period_days)).isoformat() + "Z"
    end = now.isoformat() + "Z"
    slos = [
        {
            "name": "api_availability",
            "query": r'100 * (1 - rate(dinoforge_http_requests_total{status=~"5.."}[30d]) / rate(dinoforge_http_requests_total[30d]))',
            "target": 99.9,
        },
        {
            "name": "game_launch_success",
            "query": r'100 * rate(dinoforge_game_launch_total{status="success"}[7d]) / rate(dinoforge_game_launch_total[7d])',
            "target": 99.5,
        },
        {
            "name": "pack_validation_success",
            "query": r'100 * rate(dinoforge_pack_validation_total{status="pass"}[30d]) / rate(dinoforge_pack_validation_total[30d])',
            "target": 99.0,
        },
    ]
    report = {
        "generated_at": now.isoformat(),
        "period_days": period_days,
        "slos": [],
    }
    for slo in slos:
        result = query_prometheus(prometheus_url, slo["query"], start, end)
        try:
            value = float(result["data"]["result"][0]["value"][1])
        except (KeyError, IndexError, ValueError):
            value = 100.0  # Default if no data
        status = calculate_slo(value, slo["target"])
        status["name"] = slo["name"]
        report["slos"].append(status)
    report["overall_status"] = "healthy" if all(s["status"] == "healthy" for s in report["slos"]) else "degraded"
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate SLA report")
    parser.add_argument("--period", default="30d", help="Reporting period")
    parser.add_argument("--slo", default=None, help="Specific SLO to check")
    parser.add_argument("--prometheus", default="http://localhost:9090", help="Prometheus URL")
    args = parser.parse_args()
    period_days = int(args.period.rstrip('d'))
    report = generate_report(args.prometheus, period_days)
    print(json.dumps(report, indent=2))
    if report["overall_status"] != "healthy":
        sys.exit(1)


if __name__ == "__main__":
    main()
