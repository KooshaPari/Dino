# Redaction: captured Sketchfab API token across Dino docs and scripts

- Date: 2026-11-17
- Repository: `KooshaPari/Dino` (visibility: **PUBLIC**)
- Discovered by: the closure ledger's D38.3 credential sweep, class `REAL_STORED_UNVERIFIED`
- Initially reported as 6 occurrences in 1 file; the on-branch sweep found the same value in
  **19 occurrences across 9 files**, all redacted here

## What was published

One opaque **32-character hexadecimal** token, captured from an `Authorization: Bearer` header,
appeared in three contexts:

| Context | Example shape | Count |
|---|---|---|
| `curl -H "Authorization: Bearer <value>"` download recipes | Phase 2A sourcing index (6) + infantry sourcing (1) | 7 |
| Shell exports `export SKETCHFAB_TOKEN` / `SKETCHFAB_API_TOKEN="<value>"` | archive notes, downloader report | 5 |
| Bare value in prose, prompt logs, and a script docstring | research notes, `docs/sessions/prompts_list.txt` (3), `download_priority_assets.py` | 7 |

Files affected (9): `docs/research/PHASE_2A_SOURCING_INDEX.txt` (7),
`docs/research/phase-2a-infantry-sourcing.md` (2), `docs/research/phase-2c-republic-sourcing.md` (2),
`docs/research/sketchfab-downloader-report.md` (1), `docs/archive/session-summary.md` (1),
`docs/archive/sketchfab-deliverables.md` (1),
`docs/archive/sketchfab-fix-completion-report.md` (1), `docs/sessions/prompts_list.txt` (3),
`scripts/download_priority_assets.py` (1).

All 19 were the **same single credential**. The values are not reproduced in this file, the
commit, or the pull request.

## What this change does

Every occurrence is replaced in place with the literal `<REDACTED-ROTATE-ME>`. Labels, quoting,
shell variable names, prose, and line counts are preserved, so the notes stay readable and the
download recipes stay runnable once a real token is supplied.

## Residual sweep on this branch

- `git grep -c -F` for the masked 6-character prefix of the value: **0 matches** (exit 1).
- `git grep -n -F 'Bearer '`: all hits are prose ("Bearer token authentication"), code
  (`Bearer ${token}`, `Bearer $SKETCHFAB_TOKEN`), or the placeholders introduced here. No
  `Bearer` followed by a 32-hex literal remains (targeted check exit 1).
- Whole-repo re-check for the value across git-tracked files: clean.

## What this change does NOT do

- **It does not remove the secret from history.** Every earlier commit still contains the blob.
  The token is in a public repository and must be treated as **compromised**.
- **It does not rotate the credential.** Revoke it with Sketchfab.
- History purge (`git filter-repo` plus a coordinated force-push, or repository
  archival/recreation) is an **operator decision** and is not performed by agents.

## Operator actions required

1. Revoke and rotate the Sketchfab API token.
2. Decide on history purge for the affected paths.
3. Confirm whether the token was reused in other repositories.
4. Keep new notes on `Authorization: Bearer $SKETCHFAB_TOKEN`, never a captured literal.
