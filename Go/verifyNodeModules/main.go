package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path"
	"sync"
	"time"
)

type packageJSONRoot struct {
	Version string
}

type packageLockDependency struct {
	Version      string
	Dependencies map[string]packageLockDependency
}

type packageLockJSONRoot struct {
	Dependencies map[string]packageLockDependency
}

type dependencyError struct {
	PackageName     string
	ExpectedVersion string
	ActualVersion   string
}

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
		os.Exit(-2)
		return
	}

	defer packageLockFile.Close()

	var packageLock packageLockJSONRoot
	decoder := json.NewDecoder(packageLockFile)
	if err := decoder.Decode(&packageLock); err != nil {
		_, _ = fmt.Fprintf(os.Stderr, "Could not parse 'package-lock.json': %v\n", err)
		os.Exit(-3)
		return
	}

	start := time.Now()

	var dependencyErrors []dependencyError
	var wg sync.WaitGroup
	exitCode := 0
	checkedDependencies := 0

	counter := make(chan int)
	errors := make(chan dependencyError)
	done := make(chan int)

	go func() {
		for {
			select {
			case <-counter:
				checkedDependencies++
			case err := <-errors:
				dependencyErrors = append(dependencyErrors, err)
			case <-done:
				return
			}
		}
	}()

	nodeModulesPath := path.Join(projectPath, "node_modules")

	for k, v := range packageLock.Dependencies {
		wg.Add(1)
		go func(packageName string, dependency packageLockDependency) {
			defer wg.Done()
			getDependencyErrors(packageName, dependency, nodeModulesPath, counter, errors)
		}(k, v)
	}

	wg.Wait()
	done <- 1

	elapsed := time.Since(start)

	for _, depErr := range dependencyErrors {
		_, _ = fmt.Fprintf(os.Stderr, "Version mismatch for package '%s'! Wanted '%s' but got '%s'\n", depErr.PackageName, depErr.ExpectedVersion, depErr.ActualVersion)
		exitCode++
	}

	fmt.Printf("Checked %d dependencies in %s\n", checkedDependencies, elapsed)
	os.Exit(exitCode)
}

func getDependencyErrors(packageName string, dependency packageLockDependency, nodeModulesPath string, counterChan chan int, errorsChan chan dependencyError) {
	packagePath := path.Join(nodeModulesPath, packageName)
	packageJSONPath := path.Join(packagePath, "package.json")

	counterChan <- 1

	packageJSONFile, err := os.Open(packageJSONPath)
	if err != nil {
		errorsChan <- dependencyError{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: "None"}
		return
	}

	defer packageJSONFile.Close()

	var packageJSON packageJSONRoot
	decoder := json.NewDecoder(packageJSONFile)
	if err := decoder.Decode(&packageJSON); err != nil {
		errorsChan <- dependencyError{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: "Unknown"}
		return
	}

	if packageJSON.Version != dependency.Version {
		errorsChan <- dependencyError{PackageName: packageName, ExpectedVersion: dependency.Version, ActualVersion: packageJSON.Version}
	}

	var wg sync.WaitGroup
	nestedNodeModulesPath := path.Join(packagePath, "node_modules")

	for k, v := range dependency.Dependencies {
		wg.Add(1)
		go func(packageName string, dependency packageLockDependency) {
			defer wg.Done()
			getDependencyErrors(packageName, dependency, nestedNodeModulesPath, counterChan, errorsChan)
		}(k, v)
	}

	wg.Wait()
}
