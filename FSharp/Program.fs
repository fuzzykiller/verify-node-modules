open System.Diagnostics
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open VerifyNodeModules

let serializerOptions =
    JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

let mutable checkedDependencyCount = 0

let readJsonFile<'T> path : Task<'T option> =
    task {
        try
            use fileStream = File.OpenRead path
            let! obj = JsonSerializer.DeserializeAsync<'T>(fileStream, serializerOptions)
            return Some(obj)
        with
        | :? JsonException as ex ->
            eprintfn $"Failed to deserialize %s{path}: %s{ex.Message}"
            return None
        | ex ->
            eprintfn $"Failed to open %s{path}: %s{ex.Message}"
            return None
    }

let readPackageLock =
    readJsonFile<PackageLock>

let readPackage = readJsonFile<Package>

let rec checkNestedDependencies
    (nodeModulesPath: string)
    (packageName: string)
    (dependency: PackageLockDependency)
    : Async<seq<DependencyError>> =
    async {
        let packagePath =
            Path.GetFullPath(Path.Combine(nodeModulesPath, packageName))

        let packageJsonPath =
            Path.Combine(packagePath, "package.json")

        let nestedNodeModulesPath =
            Path.Combine(packagePath, "node_modules")

        Interlocked.Increment(&checkedDependencyCount)
        |> ignore

        let! package = readPackage packageJsonPath |> Async.AwaitTask

        let packageError =
            match package with
            | Some x when x.Version <> dependency.Version ->
                Seq.singleton (DependencyError(packageName, dependency.Version, x.Version))
            | Some _ -> Seq.empty
            | None -> Seq.singleton (DependencyError(packageName, dependency.Version, "None"))

        let! nestedDependencyErrors =
            dependency.Dependencies
            |> Seq.map (fun x -> checkNestedDependencies nestedNodeModulesPath x.Key x.Value)
            |> Async.Parallel


        let dependencyErrors =
            nestedDependencyErrors
            |> Seq.concat
            |> Seq.append packageError

        return dependencyErrors
    }

let checkDependencies (projectPath: string) (packageLock: PackageLock) =
    let sw = Stopwatch.StartNew()

    let nodeModulesPath =
        Path.Combine(projectPath, "node_modules")

    try
        let dependencyErrors =
            packageLock.Dependencies
            |> Seq.map (fun x -> checkNestedDependencies nodeModulesPath x.Key x.Value)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Seq.concat

        let mutable exitCode = 0

        for dependencyError in dependencyErrors do
            exitCode <- exitCode + 1

            eprintfn
                $"Version mismatch for package '%s{dependencyError.PackageName}'! Wanted '%s{dependencyError.ExpectedVersion}' but got '%s{dependencyError.ActualVersion}'!"

        sw.Stop()

        eprintfn $"Checked %i{checkedDependencyCount} dependencies in {sw.Elapsed}"
        exitCode
    with
    | ex ->
        eprintfn $"%s{ex.Message}"
        -2

[<EntryPoint>]
let main _ =
    let projectPath =
        @"C:\Users\fuzzy\Documents\Repos\ng10-test" (* Directory.GetCurrentDirectory() *)

    let packageLockPath =
        Path.Combine(projectPath, "package-lock.json")

    let packageLock =
        readPackageLock packageLockPath
        |> Async.AwaitTask
        |> Async.RunSynchronously

    match packageLock with
    | Some x -> checkDependencies projectPath x
    | None ->
        eprintfn "'package-lock.json' not found or invalid. Aborting"
        -1


(*
if (!File.Exists(packageLockPath))
{
    Console.Error.WriteLine("'package-lock.json' not found in current directory.");
    return -1;
}

var packageLock = await JsonAccess.LoadPackageLockJson(packageLockPath);

var sw = Stopwatch.StartNew();

var nodeModulesPath = Path.Combine(projectPath, "node_modules");
var tasks = packageLock.Dependencies.Select(kvp => GetDependencyErrors(kvp.Key, kvp.Value, nodeModulesPath)).ToArray();

int exitCode = 0;
try
{
    var errors = await Task.WhenAll(tasks);
    var flattenedErrors = errors.SelectMany(x => x);

    foreach (var dependencyError in flattenedErrors)
    {
        exitCode++;

        Console.Error.WriteLine(
            "Version mismatch for package '{0}'! Wanted '{1}' but got '{2}'!",
            dependencyError.PackageName,
            dependencyError.ExpectedVersion,
            dependencyError.ActualVersion);
    }
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    exitCode = -2;
}

sw.Stop();

Console.WriteLine("Checked {0} dependencies in {1}", _checkedDependencies, sw.Elapsed);
return exitCode;
*)
