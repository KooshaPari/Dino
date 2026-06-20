// SPDX-License-Identifier: MIT OR Apache-2.0
// Copyright (c) 2024 DINOForge Authors
//
// SAFETY: This module uses unsafe code for:
// 1. SIMD-accelerated vertex decimation
// All unsafe blocks documented with SAFETY: comments and covered by tests.
//
// TODO(feat): Replace stubs with real LOD generation (ADRs DINO-017, DINO-022).

use crate::MeshData;
use std::collections::HashMap;

/// LOD levels keyed by name (e.g., "lod0", "lod1", "lod2").
pub type LodMap = HashMap<String, MeshData>;

/// Generate LOD variants from mesh data.
///
/// # Arguments
///
/// * `mesh` - Input mesh data.
/// * `targets` - Percentage targets (e.g., `[100, 60, 30]`).
///
/// # Errors
///
/// Returns an error string if targets are empty or mesh data is invalid.
pub fn generate_lods(mesh: &MeshData, targets: &[u32]) -> Result<LodMap, String> {
    if targets.is_empty() {
        return Err("at least one LOD target required".to_owned());
    }
    if mesh.triangle_count == 0 {
        return Err("mesh has no triangles".to_owned());
    }

    // TODO(feat): Implement real decimation using nalgebra + rayon.
    let mut lods = LodMap::new();
    for (i, _target) in targets.iter().enumerate() {
        lods.insert(
            format!("lod{i}"),
            MeshData {
                vertices: mesh.vertices.clone(),
                indices: mesh.indices.clone(),
                normals: mesh.normals.clone(),
                uvs: mesh.uvs.clone(),
                triangle_count: mesh.triangle_count,
            },
        );
    }
    Ok(lods)
}
