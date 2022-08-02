#if NET6_0
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace VerifyNodeModules
{
    public static partial class JsonAccess
    {
        public static partial Task<PackageJson> LoadPackageJson(string path)
        {
            return LoadJsonFileCore<PackageJson>(path, JsonContext.Default.PackageJson);
        }

        public static partial Task<PackageLockRoot> LoadPackageLockJson(string path)
        {
            return LoadJsonFileCore<PackageLockRoot>(path, JsonContext.Default.PackageLockRoot);
        }

        private static async Task<T> LoadJsonFileCore<T>(string path, JsonTypeInfo<T> serializerOptions)
        {
            await using var file = File.OpenRead(path);
            var obj = await JsonSerializer.DeserializeAsync<T>(file, serializerOptions);
            return obj;
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(PackageJson))]
    [JsonSerializable(typeof(PackageLockRoot))]
    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
#endif