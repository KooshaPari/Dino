# Dino pass 17 — generation prototype discrimination

Date 2026-09-30.

Candidate `a4409d25f71b23d92d51a55c5925bb9e48df1a84`; run `36687869773`; artifact `11084821466`; sha256 `4f278d7cb617a30e64716a5c9b385d59d9bfb65f1f88a5bd69dd8ffe7d215126`.

Six tests executed: 3 passed, 3 failed.

The **three unchanged production-path controls all failed**:
- removed content remains effective;
- identical reload accumulates conflict;
- removed patch retains stale patched value150 instead of current disk200.

The **three fresh-generation architecture prototype tests all passed** after patch-progress classification was aligned:
- fresh generation removes historical registration;
- fresh generation with patch removed consumes current disk bytes;
- failed candidate does not replace prior published generation.

This is strong isolated-SDK architecture discrimination: same repository/candidate/run, current mutable reload path red while fresh isolated generation construction/publish-on-success green on the exact counterexample classes.

It does NOT establish runtime integration. Static runtime consumers retain RegistryManager references, so consumer publication/observed-generation semantics remain the next experiment.
