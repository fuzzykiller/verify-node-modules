#if NETCOREAPP3_1
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace VerifyNodeModules
{
    public static partial class JsonAccess
    {
        public static partial Task<PackageJson> LoadPackageJson(string path)
        {
            return LoadJsonFileCore<PackageJson>(path);
        }

        public static partial Task<PackageLockRoot> LoadPackageLockJson(string path)
        {
            return LoadJsonFileCore<PackageLockRoot>(path);
        }

        private static async Task<T> LoadJsonFileCore<T>(string path)
        {
            try
            {
                await using var file = File.OpenRead(path);
                var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var obj = await JsonSerializer.DeserializeAsync<T>(file, serializerOptions);
                return obj;
            }
            catch (Exception e)
            {
                throw new JsonException(path, e);
            }
        }
    }
}
#endif