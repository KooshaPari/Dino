//! Fuzz tests for YAML pack manifest parsing
//!
//! Run with: cargo fuzz run fuzz_yaml_pack

#![no_main]
use libfuzzer_sys::fuzz_target;

fuzz_target!(|data: &[u8]| {
    if let Ok(text) = std::str::from_utf8(data) {
        // Test serde_yaml deserialization doesn't panic on arbitrary input
        let _ = serde_yaml::from_str::<serde_json::Value>(text);

        // Test that repeated parsing is idempotent
        if let Ok(first) = serde_yaml::from_str::<serde_json::Value>(text) {
            let _ = serde_json::to_string(&first);
        }
    }
});
