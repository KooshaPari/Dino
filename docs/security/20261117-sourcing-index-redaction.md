# Redaction: captured bearer token in the Phase 2A sourcing index

- Date: 2026-11-17
- Repository: `KooshaPari/Dino` (visibility: **PUBLIC**)
- File, before this change: `docs/research/PHASE_2A_SOURCING_INDEX.txt`
- Discovered by: the closure ledger's D38.3 credential sweep, class `REAL_STORED_UNVERIFIED`

## What was published

The sourcing index records the Phase 2B download commands for six Sketchfab assets. Each
entry carried a real captured HTTP header value, not a placeholder:

```
curl -H "Authorization: Bearer <value>" \
  https://api.sketchfab.com/v3/models/<model-id>/download \
```

- Occurrences: **6**, one per asset entry
- Line numbers at HEAD before this change: 19, 32, 45, 58, 71, 84
- Shape: one opaque **32-character hexadecimal** token, identical in all six lines
- Distinct values: **1** (a single credential repeated six times)

The values themselves are deliberately not reproduced here, in this file, in the commit, or
in the pull request.

## What this change does

Each of the six token values is replaced in place with the literal placeholder
`<REDACTED-ROTATE-ME>`. Labels, surrounding quotes, the `Authorization: Bearer` prefix, URLs,
and line count are unchanged, so the index stays readable and the six asset entries remain
distinguishable by their model IDs.

- Lines changed: 6 insertions, 6 deletions in one file
- Total file length unchanged (149 lines)

## What this change does NOT do

- **It does not remove the secret from history.** Every commit that ever contained the file
  still contains the blob with the live value. Anyone can read it from the public repository.
- **It does not rotate the credential.** The token must be treated as **compromised** from the
  moment it was pushed, regardless of how it was obtained.
- A second copy of the same value is recorded at line 95 of
  `docs/research/phase-2a-infantry-sourcing.md`. It is reported, not changed here.

## Operator actions required

1. **Revoke and rotate** the Sketchfab credential (it must be assumed leaked and public).
2. **Decide on history purge** (`git filter-repo` on a mirror plus a coordinated force-push,
   or repository archival/recreation). History rewriting and force-pushes are operator
   decisions and are not performed by agents.
3. Confirm whether the same token was reused anywhere else in the fleet.

Future index entries must use an environment-variable reference such as
`Authorization: Bearer $SKETCHFAB_TOKEN`, never a captured literal.
