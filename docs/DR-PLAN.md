# DINOForge Disaster Recovery Plan

## Recovery Time Objectives (RTO)
| Component | RTO | RPO |
|-----------|-----|-----|
| Git Repository | 0 (GitHub mirrors) | 0 (Git history) |
| MCP Server | 30 minutes | 5 minutes |
| Game Automation | 1 hour | 0 (stateless) |
| Monitoring Stack | 15 minutes | 5 minutes |

## Recovery Procedures

### 1. Repository Recovery
- Source of truth: GitHub (KooshaPari/Dino)
- Backup: AgilePlus submodule + local worktrees
- Recovery: `git clone` from GitHub

### 2. MCP Server Recovery
- Rebuild from source: `dotnet restore && dotnet build`
- Python deps: `pip install -e src/Tools/DinoforgeMcp`
- Config: SSOT at `config/dinoforge-ssot.yml`

### 3. Monitoring Stack Recovery
- `docker-compose up -d` from project root
- Prometheus data: persistent volume `prometheus_data`
- Grafana dashboards: `monitoring/grafana-dashboard.json`

### 4. CI/CD Recovery
- All workflows defined in `.github/workflows/`
- Re-run failed workflows: `gh workflow run <name> --repo KooshaPari/Dino`

## Testing
- DR test: Quarterly (next: 2026-11-20)
- Backup verification: Monthly
