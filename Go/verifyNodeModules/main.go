package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path"
	"time"
)

type PackageJson struct {
	Version string
}

type PackageLockDependency struct {
	Version      string
	Dependencies map[string]PackageLockDependency
}

type PackageLockRoot struct {
	Dependencies map[string]PackageLockDependency
}

type DependencyError struct {
	PackageName     string
	ExpectedVersion string
	ActualVersion   string
}

var checkedDependencies = 0

func main() {
	projectPath, err := os.Getwd()
	if err != nil {
		_, _ = fmt.Fprintf(os.Stderr, "Could not determine working directory: %v\n", err)
		os.Exit(-1)
		return
	}

	packageLockPath := path.Join(projectPath, "package-lock.json")

	packageLockFile, err := os.Open(packageLockPath)
	if err != nil {
		_, _ = fmt.Fprintf(os.Stderr, "Could not open 'package-lock.json' in current directory: %v\n", err)
		os.Exit(-1)
		return
	}

	defer packageLockFile.Close()

	var packageLock PackageLockRoot
	decoder := json.NewDecoder(packageLockFile)
	if err := decoder.Decode(&packageLock); err != nil {
		_, _ = fmt.Fprintf(os.Stderr, "Could not parse 'package-lock.json': %v\n", err)
		os.Exit(-1)
		return
	}

	start := time.Now()

	nodeModulesPath := path.Join(projectPath, "node_modules")

	var exitCode = 0
	for k, v := range packageLock.Dependencies {
		dependencyErrors := getDependencyErrors(k, v, nodeModulesPath)
		if dependencyErrors != nil {
			for _, depErr := range dependencyErrors {
				_, _ = fmt.Fprintf(os.Stderr, "Version mismatch for package '%s'! Wanted '%s' but got '%s'\n", depErr.PackageName, depErr.ExpectedVersion, depErr.ActualVersion)
				exitCode++
			}
		}
	}

	elapsed := time.Since(start)
	fmt.Printf("Checked %d dependencies in %s\n", checkedDependencies, elapsed)
	os.Exit(exitCode)
}

func getDependencyErrors(packageName string, dependency PackageLockDependency, nodeModulesPath string) []DependencyError {
	packagePath := path.Join(nodeModulesPath, packageName)
	packageJsonPath := path.Join(packagePath, "package.json")

	checkedDependencies++

	packageJsonFile, err := os.Open(packageJsonPath)
	if err != nil {
		return []DependencyError{{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: "None"}}
	}

	defer packageJsonFile.Close()

	var packageJson PackageJson
	decoder := json.NewDecoder(packageJsonFile)
	if err := decoder.Decode(&packageJson); err != nil {
		return []DependencyError{{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: "Unknown"}}
	}

	if packageJson.Version != dependency.Version {
		return []DependencyError{{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: packageJson.Version}}
	}

	nestedNodeModulesPath := path.Join(packagePath, "node_modules");

	for k, v := range dependency.Dependencies {
		nestedDependencyErrors := getDependencyErrors(k, v, nestedNodeModulesPath)
		if nestedDependencyErrors != nil {
			return nestedDependencyErrors
		}
	}

	return nil
}
