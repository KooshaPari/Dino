# Security Policy

## Reporting a Vulnerability

Do not open a public issue for a suspected vulnerability.

Preferred reporting path:

1. use GitHub private vulnerability reporting or a private security advisory if it is available for the repository
2. otherwise contact the maintainer privately before any disclosure

Please include:

- affected version or tag
- impact summary
- reproduction steps or proof of concept
- suggested mitigation if known

Target response times:

- acknowledgement within 3 business days
- initial triage within 7 business days
- status update after triage if remediation requires more time

Coordinated disclosure is expected. Public disclosure should wait until a fix or mitigation is available.

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest stable release | ✅ |
| Current pre-release under active validation | Best effort |
| Older releases | ❌ |

## Security Baseline

The repo baseline includes:

- pinned GitHub Actions where practical
- Dependabot for dependency and action updates
- Codecov coverage reporting
- SBOM and Scorecard workflows
- changelog and release-governance review on pull requests
