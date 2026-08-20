# CI Failure Response Runbook

## Triage Steps
1. Check `gh run list --repo KooshaPari/Dino --branch main --limit 10`
2. Identify the failing workflow
3. Run `gh run view <ID> --log-failed` to see the error
4. Check if it is a pre-existing issue or regression

## Common Failures
- **NU1004 lockfile drift**: Add `--force-evaluate` to `dotnet restore`
- **include-prerelease**: Remove from `actions/setup-dotnet`
- **Orphaned YAML keys**: Remove misplaced `permissions:`/`checks:` keys
- **Duplicate permissions: {}**: Remove the duplicate block
- **Broken gitlink**: Remove with `git rm --cached <path>`

## Escalation
- If CI is red for >2 hours, notify @KooshaPari
- If security workflow fails, treat as P0
