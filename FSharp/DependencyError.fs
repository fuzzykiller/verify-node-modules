namespace VerifyNodeModules

type DependencyError(packageName: string, expectedVersion: string, actualVersion: string) =
    member this.PackageName = packageName
    member this.ExpectedVersion = expectedVersion
    member this.ActualVersion = actualVersion
