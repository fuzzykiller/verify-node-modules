use std::path::Path;
use std::sync::atomic::{AtomicU32, Ordering};
use std::time::Instant;
use std::{env, fs};

use futures::executor::block_on;
use futures::future::{join_all, BoxFuture};
use futures::FutureExt;

use crate::errors::{ErrorCode, VerifyNodeModulesError};
use crate::npm_types::*;

mod errors;
mod npm_types;

static DEPENDENCY_COUNT: AtomicU32 = AtomicU32::new(0);

#[derive(Debug)]
struct DependencyError {
    package_name: String,
    expected_version: String,
    actual_version: String,
}

fn get_dependency_errors<'a>(
    package_name: &'a str,
    dependency: &'a PackageLockDependency,
    node_modules_path: &'a Path,
) -> BoxFuture<'a, Vec<DependencyError>> {
    let fut = async move {
        let package_path = node_modules_path.join(package_name);
        let package_json_path = package_path.as_path().join("package.json");
        let nested_node_modules_path = package_path.as_path().join("node_modules");

        let make_dep_err = |actual_version: &str| DependencyError {
            package_name: package_name.to_string(),
            expected_version: dependency.version.to_string(),
            actual_version: actual_version.to_string(),
        };

        DEPENDENCY_COUNT.fetch_add(1, Ordering::Relaxed);

        if !package_json_path.exists() {
            return vec![make_dep_err("None")];
        }

        let package_json_result: Result<PackageJSONRoot, VerifyNodeModulesError> =
            fs::read_to_string(package_json_path)
                .map_err(VerifyNodeModulesError::CouldNotOpenPackageJson)
                .and_then(|contents| {
                    serde_json::from_str(&contents)
                        .map_err(VerifyNodeModulesError::CouldNotParsePackageJson)
                });

        let package_json = match package_json_result {
            Ok(json_root) => json_root,
            Err(ref e) => {
                let dep_err = make_dep_err(&format!("Unknown ({})", e));

                return vec![dep_err];
            }
        };

        if package_json.version != dependency.version {
            return vec![make_dep_err(&package_json.version)];
        }

        let nested_dep_errs_future = dependency.dependencies.as_ref().map(|deps| {
            let dep_errs_futures = deps
                .iter()
                .map(|dep| get_dependency_errors(dep.0, dep.1, nested_node_modules_path.as_path()));

            join_all(dep_errs_futures)
        });

        let result = match nested_dep_errs_future {
            Some(future) => {
                let dep_errs = future.await.into_iter().flatten().collect::<Vec<_>>();

                dep_errs
            }
            None => Vec::new(),
        };

        result
    };

    fut.boxed()
}

async fn verify_node_modules() -> Result<i32, VerifyNodeModulesError> {
    let project_path = env::current_dir().map_err(VerifyNodeModulesError::CouldNotGetCwd)?;
    let package_lock_path = project_path.join("package-lock.json");

    let package_lock_contents = fs::read_to_string(package_lock_path)
        .map_err(VerifyNodeModulesError::CouldNotOpenPackageLock)?;

    let package_lock: PackageLockJSONRoot = serde_json::from_str(package_lock_contents.as_ref())
        .map_err(VerifyNodeModulesError::CouldNotParsePackageLock)?;

    let now = Instant::now();

    let node_modules_path = project_path.join("node_modules");
    let dep_errs_futures = package_lock
        .dependencies
        .iter()
        .map(|dep| get_dependency_errors(dep.0, dep.1, node_modules_path.as_path()));

    let dep_errs_future = join_all(dep_errs_futures);

    let dep_errs = dep_errs_future
        .await
        .into_iter()
        .flatten()
        .collect::<Vec<DependencyError>>();

    let mut exit_code = 0;

    for dependency_error in dep_errs {
        eprintln!(
            "Version mismatch for package '{}'! Wanted '{}' but got '{}'",
            dependency_error.package_name,
            dependency_error.expected_version,
            dependency_error.actual_version
        );

        exit_code += 1;
    }

    let elapsed = now.elapsed();
    let dependencies_checked = DEPENDENCY_COUNT.load(Ordering::Relaxed);
    println!(
        "Checked {} dependencies in {:?}",
        dependencies_checked, elapsed
    );

    Ok(exit_code)
}

fn main() {
    let result = block_on(verify_node_modules());

    match result {
        Ok(exit_code) => std::process::exit(exit_code),
        Err(e) => {
            println!("Error: {}", e);
            std::process::exit(e.error_code());
        }
    }
}
