#!/usr/bin/env python3
"""Import DINOForge Grafana dashboard."""
import json, os, sys, urllib.request, base64

GRAFANA_URL = os.environ.get("GRAFANA_URL", "http://localhost:3000")
GRAFANA_USER = os.environ.get("GRAFANA_USER", "admin")
GRAFANA_PASS = os.environ.get("GRAFANA_PASS", "admin")

def import_dashboard(path="monitoring/grafana-dashboard.json"):
    with open(path) as f: db = json.load(f)
    creds = base64.b64encode(f"{GRAFANA_USER}:{GRAFANA_PASS}".encode()).decode()
    payload = json.dumps({"dashboard": db["dashboard"], "overwrite": True}).encode()
    req = urllib.request.Request(f"{GRAFANA_URL}/api/dashboards/db", data=payload,
        headers={"Content-Type": "application/json", "Authorization": f"Basic {creds}"})
    try:
        result = json.loads(urllib.request.urlopen(req).read())
        print(f"Imported: {result.get('slug', 'unknown')}")
    except Exception as e:
        print(f"Import failed: {e}")

if __name__ == "__main__":
    import_dashboard(sys.argv[1] if len(sys.argv) > 1 else "monitoring/grafana-dashboard.json")
