# Research

- Repository contents are YAML-first content packs with a small Go smoke test in `tests/smoke_test.go`.
- There is no `go.mod`, so Go commands must run with `GO111MODULE=off`.
- `go test ./...` fails without module mode disabled.
- `GO111MODULE=off go test ./...` passes.
- `yamllint` is available locally, and the repo passes with a relaxed config that disables document-start and truthy checks and raises the line-length limit to 240.
