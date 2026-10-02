# Dino SOTA pass 2 — declarative ownership and generation consistency

Date2026-09-30. Targeted after reproduced generation failures.

## Kubernetes Server-Side Apply — LEARN FROM, do not embed

Official Kubernetes SSA tracks which manager owns fields, detects conflicts, and when a manager omits a field it previously owned, that field can be deleted/reset if no other manager owns it. This directly informs Dino's source-pack ownership/removal semantics. It also demonstrates why "append another registration" is not a sufficient declarative apply model.

Adopt concepts:
- manager/source ownership;
- omission can mean relinquish/delete;
- explicit conflicts;
- desired vs observed state/generation.

Reject direct reuse:
- Kubernetes object/field semantics are far heavier than DINO pack registries;
- Dino needs pack/content/dependency/patch graphs and host-runtime acknowledgement, not a Kubernetes API server.

Source: Kubernetes Server-Side Apply official docs, reviewed2026-09-30.

## TUF snapshot metadata — LEARN FROM

TUF snapshot metadata lists versions/hashes of metadata and ensures clients see a consistent repository view rather than mixing files from different times.

Adopt concept:
- one generation digest binds the complete pack/dependency/patch input set;
- evidence must not mix G1 pack A with G2 pack B.

Reject direct reuse:
- DINO generation publication is local product state, not software-update trust metadata;
- signatures/update roles are separate concerns.

Source: TUF Roles and metadata official docs, reviewed2026-09-30.

## Consequence

The preferred generation architecture is not novel infrastructure for its own sake. It composes established declarative-state principles with Dino-specific pack semantics:
complete desired set -> isolated candidate -> conflict/validation -> consistent generation -> publish -> observed-generation acknowledgements.
