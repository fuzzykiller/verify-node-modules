using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VerifyNodeModules
{
    public class Program
    {
        private static long _checkedDependencies;

        public static async Task<int> Main()
        {
            var projectPath = Directory.GetCurrentDirectory();
            var packageLockPath = Path.Combine(projectPath, "package-lock.json");

            if (!File.Exists(packageLockPath))
            {
                Console.Error.WriteLine("'package-lock.json' not found in current directory.");
                return -1;
            }

            var packageLock = await LoadJsonFile<PackageLockRoot>(packageLockPath);

            var sw = Stopwatch.StartNew();

            var nodeModulesPath = Path.Combine(projectPath, "node_modules");
            var tasks = packageLock.Dependencies.Select(kvp => GetDependencyErrors(kvp.Key, kvp.Value, nodeModulesPath))
                .ToArray();

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
        }

        private static async Task<IEnumerable<DependencyError>> GetDependencyErrors(string packageName,
            PackageLockDependency dependency, string nodeModulesPath)
        {
            var packagePath = Path.GetFullPath(Path.Combine(nodeModulesPath, packageName));
            var packageJsonPath = Path.Combine(packagePath, "package.json");

            Interlocked.Increment(ref _checkedDependencies);

            if (!File.Exists(packageJsonPath))
            {
                return new[] {new DependencyError(packageName, dependency.Version, "None")};
            }

            var packageJson = await LoadJsonFile<PackageJson>(packageJsonPath);
            var errors = new List<DependencyError>();

            if (packageJson.Version != dependency.Version)
            {
                errors.Add(new DependencyError(packageName, dependency.Version, packageJson.Version));
            }

            var nestedNodeModulesPath = Path.Combine(packagePath, "node_modules");
            var tasks = dependency.Dependencies
                .Select(kvp => GetDependencyErrors(kvp.Key, kvp.Value, nestedNodeModulesPath)).ToArray();

            var nestedDependencyErrors = await Task.WhenAll(tasks);

            var flattenedNestedDependencyErrors = nestedDependencyErrors.SelectMany(x => x);
            errors.AddRange(flattenedNestedDependencyErrors);

            return errors;
        }

        private static async Task<T> LoadJsonFile<T>(string path)
        {
            await using var file = File.OpenRead(path);
            var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var obj = await JsonSerializer.DeserializeAsync<T>(file, serializerOptions);
            return obj;
        }
    }
}