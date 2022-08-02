#if NET45
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        
        private static Task<T> LoadJsonFileCore<T>(string path)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var file = File.OpenText(path);
                    var serializer = new JsonSerializer();
                    var obj = (T)serializer.Deserialize(file, typeof(T));

                    return obj;
                }
                catch (Exception e)
                {
                    throw new JsonException(path, e);
                }
            });
        }
    }
}
#endif