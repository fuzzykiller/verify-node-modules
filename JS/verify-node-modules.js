const fs = require("fs");
const path = require("path");

let checkedDependencies = 0;

async function main() {
  const projectPath = process.cwd();
  const packageLockPath = path.join(projectPath, "package-lock.json");

  if (!fs.existsSync(packageLockPath)) {
    console.error("'package-lock.json' not found in current directory.");
    return -1;
  }

  /** @type import('./types').PackageLockRoot */
  const packageLock = await loadJsonFile(packageLockPath);

  const startTime = process.hrtime();

  const nodeModulesPath = path.join(projectPath, "node_modules");
  const dependencyNames = Object.keys(packageLock.dependencies);
  const tasks = dependencyNames.map((n) =>
    getDependencyErrors(n, packageLock.dependencies[n], nodeModulesPath)
  );

  let exitCode = 0;
  try {
    let nestedDependencyErrors = await Promise.all(tasks);

    /** @type import('./types').DependencyError[] */
    let flattenedErrors = [];

    for (const nestedErrors of nestedDependencyErrors) {
      flattenedErrors.push(...nestedErrors);
    }

    for (let dependencyError of flattenedErrors) {
      exitCode++;

      console.error(
        "Version mismatch for package '%s'! Wanted '%s' but got '%s'!",
        dependencyError.packageName,
        dependencyError.expectedVersion,
        dependencyError.actualVersion
      );
    }
  } catch (e) {
    console.error(e);
    exitCode = -2;
  }

  const timeToFinish = process.hrtime(startTime);

  console.log(
    "Checked %d dependencies in %s",
    checkedDependencies,
    `${timeToFinish[0]}.${timeToFinish[1].toFixed(0).padStart(9, "0")}s`
  );

  return exitCode;
}

/**
 * @param packageName {string}
 * @param dependency {import('./types').PackageLockDependency}
 * @param nodeModulesPath {string}
 */
async function getDependencyErrors(packageName, dependency, nodeModulesPath) {
  let packagePath = path.resolve(path.join(nodeModulesPath, packageName));
  let packageJsonPath = path.join(packagePath, "package.json");

  checkedDependencies++;

  if (!fs.existsSync(packageJsonPath)) {
    /** @type import('./types').DependencyError */
    const error = {
      packageName,
      expectedVersion: dependency.version,
      actualVersion: "None",
    };

    return [error];
  }

  /** @type import('./types').PackageJson */
  let packageJson = await loadJsonFile(packageJsonPath);

  /** @type import('./types').DependencyError[] */
  let errors = [];

  if (packageJson.version != dependency.version) {
    errors.push({
      packageName,
      expectedVersion: dependency.version,
      actualVersion: packageJson.version,
    });
  }

  let nestedNodeModulesPath = path.join(packagePath, "node_modules");

  if (dependency.dependencies) {
    const nestedDependencies = dependency.dependencies;
    let nestedDependencyNames = Object.keys(nestedDependencies);
    let tasks = nestedDependencyNames.map((n) =>
      getDependencyErrors(n, nestedDependencies[n], nestedNodeModulesPath)
    );

    let nestedDependencyErrors = await Promise.all(tasks);

    for (const nestedErrors of nestedDependencyErrors) {
      errors.push(...nestedErrors);
    }
  }

  return errors;
}

/**
 *
 * @param {string} path
 */
function loadJsonFile(path) {
  return new Promise((resolve, reject) => {
    fs.readFile(path, { encoding: "utf-8" }, (err, data) => {
      if (err) {
        reject(err);
      } else {
        const obj = JSON.parse(data);
        resolve(obj);
      }
    });
  });
}

main().then((exitCode) => process.exit(exitCode));
