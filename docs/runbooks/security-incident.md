# Security Incident Response Runbook

## Detection
- Trufflehog scan failure
- CodeQL alert
- Secret scan finding
- Dependabot security alert

## Response Steps
1. **Assess**: Determine severity (P0-P3)
2. **Contain**: Revoke compromised credentials immediately
3. **Investigate**: Run `gh api repos/KooshaPari/Dino/code-scanning/alerts`
4. **Remediate**: Fix the vulnerability
5. **Verify**: Re-run security workflows
6. **Document**: Update SECURITY.md if needed

## Severity Levels
- P0: Active exploit, credential leak -> Stop everything
- P1: Vulnerable dependency with known CVE -> Fix within 24h
- P2: Code quality issue with security implications -> Fix within 1 week
- P3: Hardening opportunity -> Fix in next sprint

## Contacts
- Primary: @KooshaPari
