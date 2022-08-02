using System.Threading.Tasks;

namespace VerifyNodeModules
{
    public static partial class JsonAccess
    {
        public static partial Task<PackageJson> LoadPackageJson(string path);
        public static partial Task<PackageLockRoot> LoadPackageLockJson(string path);
    }
}