// SPDX-License-Identifier: MIT OR Apache-2.0
// Copyright (c) 2024 DINOForge Authors
//
// SAFETY: This module uses unsafe code for:
// 1. Direct Assimp FFI bindings (null pointer checks, valid UTF-8 assumptions)
// All unsafe blocks documented with SAFETY: comments and covered by tests.
//
// TODO(feat): Replace stubs with real Assimp FFI bindings (ADRs DINO-017, DINO-022).

use std::fmt;

/// A triangle mesh in the scene (Assimp `aiMesh` equivalent).
#[derive(Debug, Clone)]
pub struct Mesh {
    pub name: String,
    pub vertices: Vec<[f32; 3]>,
    pub normals: Vec<[f32; 3]>,
    pub uvs: Vec<[f32; 2]>,
    pub indices: Vec<u32>,
    pub bones: Vec<Bone>,
    pub material_index: usize,
}

/// Bone/weight data (Assimp `aiBone` equivalent).
#[derive(Debug, Clone)]
pub struct Bone {
    pub name: String,
    pub weights: Vec<VertexWeight>,
}

/// Vertex weight for skinning.
#[derive(Debug, Clone)]
pub struct VertexWeight {
    pub vertex_index: usize,
    pub weight: f32,
}

/// Scene material (Assimp `aiMaterial` equivalent).
#[derive(Debug, Clone)]
pub struct Material {
    pub name: String,
}

/// Scene animation (Assimp `aiAnimation` equivalent).
#[derive(Debug, Clone)]
pub struct Animation {
    pub name: String,
    pub duration_ticks: f64,
}

/// Loaded scene graph (Assimp `aiScene` equivalent).
#[derive(Debug, Clone)]
pub struct Scene {
    pub meshes: Vec<Mesh>,
    pub materials: Vec<Material>,
    pub animations: Vec<Animation>,
}

/// Errors from scene loading.
#[derive(Debug)]
pub enum SceneError {
    FileNotFound(String),
    ParseFailed(String),
}

impl fmt::Display for SceneError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            SceneError::FileNotFound(p) => write!(f, "file not found: {p}"),
            SceneError::ParseFailed(msg) => write!(f, "parse failed: {msg}"),
        }
    }
}

impl std::error::Error for SceneError {}

/// Load a 3D scene from file using Assimp.
///
/// # Errors
///
/// Returns `SceneError::FileNotFound` if the path does not exist,
/// or `SceneError::ParseFailed` if Assimp cannot read the file.
pub fn load_scene(path: &str) -> Result<Scene, SceneError> {
    if !std::path::Path::new(path).exists() {
        return Err(SceneError::FileNotFound(path.to_owned()));
    }
    // TODO(feat): Replace with real Assimp FFI via assimp-sys or ffi wrapper.
    Err(SceneError::ParseFailed(format!(
        "Assimp binding not yet implemented: {path}"
    )))
}
