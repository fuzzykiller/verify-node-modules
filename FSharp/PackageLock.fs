namespace VerifyNodeModules

type PackageLock(dependencies: Map<string, PackageLockDependency>) =
    member this.Dependencies = dependencies