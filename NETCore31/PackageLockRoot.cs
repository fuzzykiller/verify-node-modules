using System.Collections.Generic;

namespace VerifyNodeModules
{
    public class PackageLockRoot
    {
        public Dictionary<string, PackageLockDependency> Dependencies { get; set; }
    }
}