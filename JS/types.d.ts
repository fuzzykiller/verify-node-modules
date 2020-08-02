interface DependencyDictionary {
    [name: string]: PackageLockDependency;
}

export interface PackageJson {
    readonly version: string;
}

export interface PackageLockDependency {
    readonly version: string;
    readonly dependencies?: DependencyDictionary;
}

export interface PackageLockRoot {
    readonly dependencies: DependencyDictionary;
}

export interface DependencyError {
    readonly packageName: string;
    readonly expectedVersion: string;
    readonly actualVersion: string;
}