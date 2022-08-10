namespace VerifyNodeModules

type PackageLockDependency(version: string, dependencies: Map<string, PackageLockDependency>) =

    let coalesce x replacement =
        if not (obj.ReferenceEquals(x, null)) then
            x
        else
            replacement

    member this.Version = version

    member this.Dependencies =
        coalesce dependencies Map.empty
