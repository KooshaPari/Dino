// SPDX-License-Identifier: MIT OR Apache-2.0
// Copyright (c) 2024 DINOForge Authors
//
// SAFETY: This module uses unsafe code for:
// 1. Mesh data slicing (invariants documented per operation)
// All unsafe blocks documented with SAFETY: comments and covered by tests.
//
// TODO(feat): Replace stubs with real mesh processing (ADRs DINO-017, DINO-022).

use crate::assimp_bind;
use crate::{BoundsData, MaterialData, MeshData, SkeletonData};

/// Combine all scene meshes into a single mesh.
///
/// # Errors
///
/// Returns an error string if the scene has no meshes or the mesh data is
/// inconsistent.
pub fn combine_meshes(scene: &assimp_bind::Scene) -> Result<MeshData, String> {
    if scene.meshes.is_empty() {
        return Err("scene contains no meshes".to_owned());
    }
    // TODO(feat): Implement actual vertex/index aggregation.
    let total_tris: usize = scene.meshes.iter().map(|m| m.indices.len() / 3).sum();
    Ok(MeshData {
        vertices: vec![],
        indices: vec![],
        normals: None,
        uvs: None,
        triangle_count: total_tris,
    })
}

/// Extract materials from the scene.
pub fn extract_materials(scene: &assimp_bind::Scene) -> Vec<MaterialData> {
    scene
        .materials
        .iter()
        .map(|m| MaterialData {
            name: m.name.clone(),
            color: None,
            metallic: None,
            roughness: None,
        })
        .collect()
}

/// Extract skeleton from the first skinned mesh.
pub fn extract_skeleton(scene: &assimp_bind::Scene) -> Option<SkeletonData> {
    let mesh = scene.meshes.iter().find(|m| !m.bones.is_empty())?;
    let bones = mesh
        .bones
        .iter()
        .enumerate()
        .map(|(i, b)| crate::BoneData {
            name: b.name.clone(),
            parent_index: if i == 0 { None } else { Some(i - 1) },
        })
        .collect();
    Some(SkeletonData {
        name: mesh.name.clone(),
        bones,
    })
}

/// Compute bounding box from vertex data.
#[allow(dead_code)]
pub fn compute_bounds(vertices: &[[f32; 3]]) -> BoundsData {
    let mut min = [f32::MAX, f32::MAX, f32::MAX];
    let mut max = [f32::MIN, f32::MIN, f32::MIN];
    for v in vertices {
        min[0] = min[0].min(v[0]);
        min[1] = min[1].min(v[1]);
        min[2] = min[2].min(v[2]);
        max[0] = max[0].max(v[0]);
        max[1] = max[1].max(v[1]);
        max[2] = max[2].max(v[2]);
    }
    BoundsData { min, max }
}
