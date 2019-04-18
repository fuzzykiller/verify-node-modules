using System.Collections.Generic;

namespace VerifyNodeModules
{
    public class PackageLockDependency
    {
        public string Version { get; set; }
        public Dictionary<string, PackageLockDependency> Dependencies { get; set; } = new Dictionary<string, PackageLockDependency>();
    }
}