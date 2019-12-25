use serde::Deserialize;
use std::collections::HashMap;

/// Represents the package.json root object
#[derive(Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct PackageJSONRoot {
    pub version: String
}

/// Represents a package-lock.json dependency object
#[derive(Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct PackageLockDependency {
    pub version: String,
    
    /// Dependencies can optionally contain dependencies of their own
    #[serde(default)]
    pub dependencies: Option<HashMap<String, PackageLockDependency>>,
}

/// Represents the package-lock.json root object
#[derive(Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct PackageLockJSONRoot {
    pub dependencies: HashMap<String, PackageLockDependency>
}