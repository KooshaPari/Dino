# Release Process Runbook

## Prerequisites
- All CI workflows green on main
- CHANGELOG updated
- Version bumped (semver-bump.yml or manual)

## Steps
1. Verify CI: `gh run list --repo KooshaPari/Dino --branch main --limit 5`
2. Update version in source files
3. Update CHANGELOG.md
4. Commit: `git commit -m "release: vX.Y.Z"`
5. Tag: `git tag vX.Y.Z`
6. Push: `git push origin main --tags`
7. Create GitHub Release: `gh release create vX.Y.Z --title "vX.Y.Z" --notes "Release notes"`
8. Verify release drafter picked it up

## Post-Release
- Monitor CI for regressions
- Update downstream consumers
- Archive old worktrees if needed
