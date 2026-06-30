//! DINOForge asset pipeline — the engine behind in-game model swaps.
//!
//! Language tiering (locked doctrine): this Rust crate owns **mesh I/O + graph**
//! (glTF parse, scene-graph aggregation, LOD via meshoptimizer SIMD CPU decimation).
//! GPU-shaped kernels (Garland-Heckbert quadrics, skinning, batch transforms) are the
//! Mojo tier (MAX + GPU) — see `docs/asset-engine-mojo-plan.md`. The old AssetPipelineZig
//! Garland-Heckbert / BVH TODO stubs are superseded by this real CPU path + the Mojo plan.
//!
//! Wrap, don't handroll (CLAUDE.md): glTF import wraps the `gltf` crate; LOD/decimate
//! wraps `meshopt` (meshoptimizer Rust bindings, industry standard).
//!
//! ABI: the C# Runtime (`RustAssetPipelineInterop.cs`) consumes this via cdecl P/Invoke
//! (`RustGetVersion` / `RustImportAsset` / `RustOptimizeAsset` / `RustFreeString`). Those
//! `extern "C"` exports are the real, in-use boundary — PyO3 is intentionally not used here.

use serde::{Deserialize, Serialize};
use std::ffi::{c_char, c_int, CStr, CString};
use std::path::Path;

// ===== Data models (mirror C# AssetData / MeshData JSON) =====

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ImportedAsset {
    pub asset_id: String,
    pub source_path: String,
    pub mesh: MeshData,
    pub materials: Vec<MaterialData>,
    pub metadata: AssetMetadata,
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct MeshData {
    /// Flat array: [x0, y0, z0, x1, y1, z1, ...]
    pub vertices: Vec<f32>,
    pub indices: Vec<u32>,
    pub normals: Option<Vec<f32>>,
    pub uvs: Option<Vec<f32>>,
    pub triangle_count: usize,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MaterialData {
    pub name: String,
    pub color: Option<[f32; 4]>,
    pub metallic: Option<f32>,
    pub roughness: Option<f32>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetMetadata {
    pub poly_count: usize,
    pub material_count: usize,
    pub bounds: Option<BoundsData>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BoundsData {
    pub min: [f32; 3],
    pub max: [f32; 3],
}

/// Request shape for RustOptimizeAsset (mirrors C# OptimizeRequest).
#[derive(Debug, Clone, Deserialize)]
pub struct OptimizeRequest {
    pub mesh: MeshData,
    /// LOD targets as percentages of original triangle count, e.g. [100, 60, 30].
    pub lod_targets: Vec<u32>,
}

// ===== Real glTF import (wraps the `gltf` crate) =====

/// Import a glTF/GLB file into a single aggregated mesh. Returns real vertices/indices.
pub fn import_gltf(path: &Path) -> Result<ImportedAsset, String> {
    let (document, buffers, _images) =
        gltf::import(path).map_err(|e| format!("gltf import failed: {e}"))?;

    let mut vertices: Vec<f32> = Vec::new();
    let mut normals: Vec<f32> = Vec::new();
    let mut uvs: Vec<f32> = Vec::new();
    let mut indices: Vec<u32> = Vec::new();
    let mut have_normals = true;
    let mut have_uvs = true;

    let mut bmin = [f32::INFINITY; 3];
    let mut bmax = [f32::NEG_INFINITY; 3];

    for mesh in document.meshes() {
        for prim in mesh.primitives() {
            let reader = prim.reader(|b| Some(&buffers[b.index()]));
            let base = (vertices.len() / 3) as u32;

            let positions: Vec<[f32; 3]> = match reader.read_positions() {
                Some(p) => p.collect(),
                None => continue, // skip primitives without geometry
            };
            for p in &positions {
                for i in 0..3 {
                    bmin[i] = bmin[i].min(p[i]);
                    bmax[i] = bmax[i].max(p[i]);
                }
                vertices.extend_from_slice(p);
            }

            match reader.read_normals() {
                Some(n) => {
                    for v in n {
                        normals.extend_from_slice(&v);
                    }
                }
                None => have_normals = false,
            }
            match reader.read_tex_coords(0) {
                Some(t) => {
                    for v in t.into_f32() {
                        uvs.extend_from_slice(&v);
                    }
                }
                None => have_uvs = false,
            }

            match reader.read_indices() {
                Some(idx) => {
                    for i in idx.into_u32() {
                        indices.push(base + i);
                    }
                }
                None => {
                    // No index buffer: sequential triangles.
                    for i in 0..positions.len() as u32 {
                        indices.push(base + i);
                    }
                }
            }
        }
    }

    if vertices.is_empty() {
        return Err("gltf contained no mesh geometry".into());
    }

    let materials: Vec<MaterialData> = document
        .materials()
        .map(|m| {
            let pbr = m.pbr_metallic_roughness();
            MaterialData {
                name: m.name().unwrap_or("material").to_string(),
                color: Some(pbr.base_color_factor()),
                metallic: Some(pbr.metallic_factor()),
                roughness: Some(pbr.roughness_factor()),
            }
        })
        .collect();

    let triangle_count = indices.len() / 3;
    let material_count = materials.len();

    Ok(ImportedAsset {
        asset_id: String::new(),
        source_path: path.to_string_lossy().into_owned(),
        mesh: MeshData {
            vertices,
            indices,
            normals: have_normals.then_some(normals),
            uvs: have_uvs.then_some(uvs),
            triangle_count,
        },
        materials,
        metadata: AssetMetadata {
            poly_count: triangle_count,
            material_count,
            bounds: Some(BoundsData {
                min: bmin,
                max: bmax,
            }),
        },
    })
}

// ===== Real LOD decimation (wraps `meshopt` = meshoptimizer SIMD CPU) =====

/// Decimate a mesh to `target_index_count` indices via meshoptimizer's
/// `simplify` (collapses edges, error-bounded). Returns the simplified MeshData.
pub fn decimate(mesh: &MeshData, target_index_count: usize) -> Result<MeshData, String> {
    if mesh.indices.is_empty() || mesh.vertices.len() < 9 {
        return Err("mesh has insufficient geometry to decimate".into());
    }

    // VertexDataAdapter over the flat position array (stride = 3 f32 = 12 bytes).
    let vertex_bytes: &[u8] = f32_slice_as_bytes(&mesh.vertices);
    let adapter = meshopt::VertexDataAdapter::new(vertex_bytes, 12, 0)
        .map_err(|e| format!("meshopt adapter failed: {e}"))?;

    let target = target_index_count.max(3).min(mesh.indices.len());
    let target_error = 1e-2_f32;
    let mut result_error = 0.0_f32;
    let simplified = meshopt::simplify(
        &mesh.indices,
        &adapter,
        target,
        target_error,
        meshopt::SimplifyOptions::None,
        Some(&mut result_error),
    );

    let triangle_count = simplified.len() / 3;
    Ok(MeshData {
        vertices: mesh.vertices.clone(),
        indices: simplified,
        normals: mesh.normals.clone(),
        uvs: mesh.uvs.clone(),
        triangle_count,
    })
}

/// Generate LOD chain from percentage targets (e.g. [100, 60, 30]).
fn generate_lods(mesh: &MeshData, targets: &[u32]) -> Result<Vec<MeshData>, String> {
    let base = mesh.indices.len();
    let mut out = Vec::with_capacity(targets.len());
    for &pct in targets {
        let target_idx = ((base as u64 * pct as u64) / 100) as usize;
        out.push(decimate(mesh, target_idx)?);
    }
    Ok(out)
}

/// Reinterpret &[f32] as &[u8] for the meshopt adapter. SAFETY: f32 has no invalid
/// bit patterns for reads, and the returned slice borrows `data` unchanged.
fn f32_slice_as_bytes(data: &[f32]) -> &[u8] {
    unsafe { std::slice::from_raw_parts(data.as_ptr() as *const u8, std::mem::size_of_val(data)) }
}

// ===== C-ABI exports (the real boundary consumed by C# RustAssetPipelineInterop) =====

const VERSION: &str = concat!("dinoforge-asset-pipeline ", env!("CARGO_PKG_VERSION"));

fn to_c_string(s: String) -> *mut c_char {
    CString::new(s)
        .map(CString::into_raw)
        .unwrap_or(std::ptr::null_mut())
}

/// Returns an allocated version C-string via `out`. 0 = ok.
#[no_mangle]
pub extern "C" fn RustGetVersion(out: *mut *mut c_char) -> c_int {
    if out.is_null() {
        return 1;
    }
    unsafe { *out = to_c_string(VERSION.to_string()) };
    0
}

/// Import a glTF/GLB asset; writes JSON of ImportedAsset to `out`. 0 = ok.
/// # Safety: `file_path`/`asset_id` must be valid NUL-terminated C strings.
#[no_mangle]
pub extern "C" fn RustImportAsset(
    file_path: *const c_char,
    asset_id: *const c_char,
    out: *mut *mut c_char,
) -> c_int {
    let result = std::panic::catch_unwind(|| {
        if file_path.is_null() || out.is_null() {
            return Err("null argument".to_string());
        }
        let path = unsafe { CStr::from_ptr(file_path) }
            .to_str()
            .map_err(|e| e.to_string())?;
        let id = if asset_id.is_null() {
            String::new()
        } else {
            unsafe { CStr::from_ptr(asset_id) }
                .to_str()
                .map_err(|e| e.to_string())?
                .to_string()
        };
        let mut asset = import_gltf(Path::new(path))?;
        asset.asset_id = id;
        serde_json::to_string(&asset).map_err(|e| e.to_string())
    });

    match result {
        Ok(Ok(json)) => {
            unsafe { *out = to_c_string(json) };
            0
        }
        _ => 2,
    }
}

/// Optimize/decimate: input JSON = OptimizeRequest, output JSON = Vec<MeshData> (LOD chain).
#[no_mangle]
pub extern "C" fn RustOptimizeAsset(request_json: *const c_char, out: *mut *mut c_char) -> c_int {
    let result = std::panic::catch_unwind(|| {
        if request_json.is_null() || out.is_null() {
            return Err("null argument".to_string());
        }
        let json = unsafe { CStr::from_ptr(request_json) }
            .to_str()
            .map_err(|e| e.to_string())?;
        let req: OptimizeRequest = serde_json::from_str(json).map_err(|e| e.to_string())?;
        let lods = generate_lods(&req.mesh, &req.lod_targets)?;
        serde_json::to_string(&lods).map_err(|e| e.to_string())
    });

    match result {
        Ok(Ok(json)) => {
            unsafe { *out = to_c_string(json) };
            0
        }
        _ => 2,
    }
}

/// Free a C-string previously returned by this library.
/// # Safety: `ptr` must come from one of this lib's exports (or be null).
#[no_mangle]
pub extern "C" fn RustFreeString(ptr: *mut c_char) {
    if !ptr.is_null() {
        unsafe { drop(CString::from_raw(ptr)) };
    }
}

// ===== Tests =====

#[cfg(test)]
mod tests {
    use super::*;

    /// Minimal on-disk glTF 2.0 (single triangle) — proves the import stub is gone.
    fn triangle_gltf() -> (tempfile::TempDir, std::path::PathBuf) {
        let dir = tempfile::tempdir().unwrap();
        let positions: [f32; 9] = [0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0];
        let idx: [u16; 3] = [0, 1, 2];
        let mut bin = Vec::new();
        bin.extend_from_slice(f32_slice_as_bytes(&positions));
        let idx_off = bin.len();
        for i in idx {
            bin.extend_from_slice(&i.to_le_bytes());
        }
        let bin_path = dir.path().join("tri.bin");
        std::fs::write(&bin_path, &bin).unwrap();

        let gltf = format!(
            r#"{{
  "asset": {{"version": "2.0"}},
  "buffers": [{{"uri": "tri.bin", "byteLength": {total}}}],
  "bufferViews": [
    {{"buffer": 0, "byteOffset": 0, "byteLength": 36, "target": 34962}},
    {{"buffer": 0, "byteOffset": {idx_off}, "byteLength": 6, "target": 34963}}
  ],
  "accessors": [
    {{"bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3",
      "min": [0,0,0], "max": [1,1,0]}},
    {{"bufferView": 1, "componentType": 5123, "count": 3, "type": "SCALAR"}}
  ],
  "meshes": [{{"primitives": [{{"attributes": {{"POSITION": 0}}, "indices": 1}}]}}],
  "nodes": [{{"mesh": 0}}],
  "scenes": [{{"nodes": [0]}}]
}}"#,
            total = bin.len(),
            idx_off = idx_off
        );
        let gltf_path = dir.path().join("tri.gltf");
        std::fs::write(&gltf_path, gltf).unwrap();
        (dir, gltf_path)
    }

    #[test]
    fn import_real_gltf_yields_geometry() {
        let (_dir, path) = triangle_gltf();
        let asset = import_gltf(&path).expect("import should succeed on valid glTF");
        // Proves the stub is gone: real vertices/triangles parsed.
        assert_eq!(asset.mesh.vertices.len(), 9, "3 verts * 3 floats");
        assert_eq!(asset.mesh.indices.len(), 3);
        assert!(asset.mesh.triangle_count > 0);
        assert!(asset.metadata.bounds.is_some());
    }

    #[test]
    fn decimate_returns_real_mesh() {
        // Two-triangle quad; decimate to half the indices.
        let mesh = MeshData {
            vertices: vec![
                0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0, 0.0,
            ],
            indices: vec![0, 1, 2, 0, 2, 3],
            normals: None,
            uvs: None,
            triangle_count: 2,
        };
        let lod = decimate(&mesh, 3).expect("decimate should succeed");
        assert!(!lod.indices.is_empty(), "decimated mesh must keep geometry");
        assert!(lod.triangle_count >= 1);
        assert!(lod.indices.len() <= mesh.indices.len());
    }

    #[test]
    fn generate_lod_chain() {
        let mesh = MeshData {
            vertices: vec![
                0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0, 0.0,
            ],
            indices: vec![0, 1, 2, 0, 2, 3],
            normals: None,
            uvs: None,
            triangle_count: 2,
        };
        let lods = generate_lods(&mesh, &[100, 60, 30]).unwrap();
        assert_eq!(lods.len(), 3);
        assert!(lods[0].triangle_count > 0);
    }
}
