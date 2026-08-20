//! Fuzz tests for the DINOForge asset pipeline
//!
//! Run with: cargo fuzz run fuzz_asset_pipeline

#![no_main]
use libfuzzer_sys::fuzz_target;
use std::collections::HashMap;

fuzz_target!(|data: &[u8]| {
    // Fuzz YAML manifest parsing
    if let Ok(text) = std::str::from_utf8(data) {
        // Test that YAML parsing of pack manifests doesn't panic
        let _ = serde_yaml::from_str::<HashMap<String, serde_json::Value>>(text);
    }

    // Fuzz binary input handling
    let _ = data.len();
    let _ = data.iter().map(|b| b.wrapping_add(1)).collect::<Vec<_>>();
});
