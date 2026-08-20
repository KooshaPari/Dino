# DINOForge Incident Response Playbook

## Severity Classification
| Level | Description | Response Time | Example |
|-------|-------------|---------------|----------|
| SEV-1 | Service down, data loss risk | 15 minutes | MCP server unreachable |
| SEV-2 | Major feature broken | 1 hour | Game launch validation fails |
| SEV-3 | Minor feature degraded | 4 hours | Dashboard stale |
| SEV-4 | Cosmetic/non-blocking | Next sprint | Lint warning |

## Response Process

### 1. Detect
- Monitoring alerts (Prometheus/Grafana)
- CI failure notification
- User report

### 2. Triage
- Assign severity level
- Identify affected component
- Check if rollback is needed

### 3. Mitigate
- SEV-1: Revert to last known good
- SEV-2: Hotfix branch
- SEV-3: Fix in next PR
- SEV-4: Backlog

### 4. Resolve
- Fix the root cause
- Add regression test
- Update monitoring if needed

### 5. Review
- Post-incident review (SEV-1/2 only)
- Update runbooks
- Share learnings

## Communication
- SEV-1: GitHub issue + direct message
- SEV-2: GitHub issue
- SEV-3/4: PR description

## Escalation Path
1. @KooshaPari (sole maintainer)
2. Community contributors via GitHub issues
