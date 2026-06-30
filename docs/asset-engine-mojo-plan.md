# Asset Engine — Mojo GPU-Kernel Tier (Plan)

Status: **PLAN ONLY** — `mojo`/MAX not installed on this machine (verified: `mojo --version` → not found,
`which mojo` → not found). The landed CPU fix is Rust + `meshopt` (meshoptimizer SIMD). This doc designs the
Mojo GPU-kernel upgrade for user approval before any `modular`/MAX install.

## Why this tier exists (language doctrine, locked)

Split the asset-swap engine by workload:

| Workload | Tier | Crate / runtime | Rationale |
|---|---|---|---|
| glTF/GLB parse, scene-graph aggregation, BVH build | **Rust** | `gltf`, (future) `meshopt`/`bvh` | Correctness + CPU; wrap mature crates |
| Mesh decimation (Garland-Heckbert quadrics), skinning, batch transforms | **Mojo** | MAX + GPU | Data-parallel, GPU-shaped; beats Zig manual-SPIR-V |

The old `src/Tools/AssetPipelineZig` Garland-Heckbert decimation + BVH TODO stubs are **superseded**: the CPU
decimation path now lives in Rust (`meshopt::simplify`), and the GPU quadric-error decimation is a **Mojo
candidate, not Zig**. Zig manual SPIR-V is not pursued.

## Kernels that move to Mojo

1. **Quadric-error mesh decimation (Garland-Heckbert)** — per-vertex 4x4 quadric matrices, edge-collapse cost
   evaluation. Embarrassingly parallel over vertices/edges; GPU reduction for cost heap. (CPU fallback already
   shipped via `meshopt::simplify`.)
2. **Linear-blend skinning / batch transforms** — `out_v = Σ w_i · (M_i · v)` per vertex per frame. Pure SIMD/GPU.
3. **BVH leaf-bounds + Morton-code sort** — parallel AABB reduction + radix sort for spatial index build.

## Rust ↔ Mojo C-ABI boundary

Mojo exports a C-ABI shared lib (`.dll`/`.so`/`.dylib`); Rust calls it via `extern "C"` (mirrors how C# calls
Rust today). Flat POD buffers only across the boundary — no Rust/Mojo types shared.

```
// Mojo side (exported C ABI):
//   i32 mojo_decimate_qem(
//       const f32* positions, u64 vertex_count,   // xyz triplets
//       const u32* indices,   u64 index_count,
//       u64 target_index_count,
//       u32* out_indices, u64* out_index_count)   // caller-allocated, capacity = index_count

#[link(name = "dinoforge_asset_kernels")]
extern "C" {
    fn mojo_decimate_qem(
        positions: *const f32, vertex_count: u64,
        indices: *const u32, index_count: u64,
        target_index_count: u64,
        out_indices: *mut u32, out_index_count: *mut u64,
    ) -> i32;
}
```

Rust `decimate()` gains a runtime switch: if the Mojo kernel lib is present (and a GPU is available), dispatch
to `mojo_decimate_qem`; otherwise fall back to the shipped `meshopt::simplify` CPU path. Same JSON in/out, so the
C# Runtime and PyO3/MCP surfaces are unchanged.

## MAX / GPU offload targets (user hardware)

- **RTX 3090 Ti** — CUDA / DX12; primary GPU decimation + skinning target. MAX emits PTX.
- **M1 MacBook** — Metal; MAX Metal backend. Same Mojo source, different MAX target.
- CPU SIMD (`meshopt`) remains the universal fallback when no GPU / MAX runtime.

## Install needed (USER APPROVAL REQUIRED)

```
# Modular / MAX toolchain (provides `mojo` + MAX GPU runtime):
curl -ssL https://magic.modular.com/ | bash      # or: pixi global install modular
modular install max
mojo --version                                   # verify
```

Flag: **mojo/max install needed via `modular`** — not installed; deferred to user. Until then the Rust + meshopt
CPU path is the landed, building, tested implementation.
