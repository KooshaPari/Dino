# ADR-010: Deterministic Star-Wars-Style Asset Intake Pipeline

**Status**: Proposed
**Date**: 2026-03-11
**Deciders**: Agent Org

## Context

The current mod workflow can ingest assets through ad-hoc manual steps, but a repeatable low-poly conversion for a Star Wars conversion requires:

- deterministic source discovery (prefer API-driven),
- provenance-first manifests,
- explicit legal/IP state separate from technical quality,
- machine-readable scoring and rejection rules,
- and non-destructive registration before runtime use.

The immediate need is a pre-implementation contract for an `assetctl` toolchain, not a full production asset conversion subsystem.

## Decision

- Add an explicit agent-facing intake pipeline with machine-readable schemas and policy files.
- Add `assetctl` as a dedicated CLI surface for `search`, `intake`, `normalize`, `validate`, `stylize`, `register`, `export-unity`.
- Use a source-tier model (Sketchfab primary, BlendSwap secondary, ModDB reference, browser fallback last).
- Track two independent state dimensions on each asset:
  - `technical_status` (discovered → normalized → validated → ready_for_prototype),
  - `ip_status` (generic_safe vs high_risk_do_not_ship).
- Require a manifest conforming to `schemas/asset-manifest.schema.json` before promotion beyond prototype.

## Consequences

- Legal/provenance decisions become auditable and machine-enforced instead of informal.
- Asset quality variance is controlled through explicit technical gates and validation reports.
- Source/API strategy is configurable without code changes via policy documents.
- Pre-implementation artifacts remain non-invasive: no runtime coupling before design freeze.
- Future release-safe workflows can be added by changing policy and status transitions, not by rewriting the whole pipeline.

## Alternatives Considered

- One-off manual asset import:
  - rejected due to non-repeatability and weak provenance control.
- Full external DAM integration:
  - rejected in V1 due to complexity and scope, but preserved as a future extension.
- Browser automation as default:
  - rejected because it is brittle and harder to audit for policy and retries.
