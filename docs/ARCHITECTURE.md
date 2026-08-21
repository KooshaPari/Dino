# DINOForge Architecture

## System Overview

DINOForge is a modding platform for Diplomacy is Not an Option that provides:
- **MCP Server** (Python) - Game automation, asset pipeline, catalog tools
- **Mod Framework** (C#) - Unity integration, pack system, ECS bridge
- **Asset Pipeline** (Rust) - Import, validate, optimize, LOD generation
- **Game Bridge** - Named pipe IPC for real-time game interaction

## Component Architecture

```
┌─────────────────────────────────────────────────┐
│                  MCP Server                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐ │
│  │ REST API │ │ FastMCP  │ │ OpenTelemetry    │ │
│  │ (FastAPI)│ │ Protocol │ │ (tracing.py)     │ │
│  └────┬─────┘ └────┬─────┘ └──────────────────┘ │
│       │             │                            │
│  ┌────┴─────────────┴──────────────────────────┐ │
│  │              Tool Registry                   │ │
│  │  game_control │ asset_pipeline │ catalog    │ │
│  │  log_analysis │ voice_commands │ routes     │ │
│  └───────────────┴───────────────┴────────────┘ │
│                     │                            │
│  ┌──────────────────┴──────────────────────────┐ │
│  │          Cross-Cutting Concerns              │ │
│  │  audit_logger │ rate_limiter │ sentry       │ │
│  │  i18n │ a11y │ config │ isolation_layer     │ │
│  └─────────────────────────────────────────────┘ │
└────────────────────────┬────────────────────────┘
                         │ Named Pipe IPC
┌────────────────────────┴────────────────────────┐
│              Game Bridge (DINOForge)              │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐ │
│  │ Pack     │ │ Unit     │ │ Wave/Doctrine    │ │
│  │ System   │ │ Registry │ │ System           │ │
│  └──────────┘ └──────────┘ └──────────────────┘ │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│              Asset Pipeline (Rust)               │
│  Import → Validate → Optimize → LOD → Prefab   │
└─────────────────────────────────────────────────┘
```

## Data Flow
1. **MCP Tool Call** → Tool Registry → Module Handler → Game Bridge (pipe)
2. **Asset Import** → Rust Pipeline → Addressable Catalog → Unity
3. **REST Request** → FastAPI → Tool Registry → Module Handler → Response
4. **Game Event** → Named Pipe → MCP Handler → Tool Output

## Deployment Architecture
- **Local**: Docker Compose (Prometheus, Grafana, Loki, Alertmanager)
- **Staging**: Argo Rollouts canary on EKS (10% → 25% → 50% → 75% → 100%)
- **Production**: Same as staging with HPA (2-10 replicas)
- **CI/CD**: GitHub Actions (52 workflows) → ECR → EKS

## Security Architecture
- **Secret Scanning**: Trufflehog + CodeQL
- **Dependency Audit**: Dependabot + cargo-deny
- **Agent Isolation**: isolation_layer.py (6 modules)
- **Rate Limiting**: Token bucket per agent
- **Audit Logging**: Structured events with actor/resource/action
- **Container Scanning**: Trivy
