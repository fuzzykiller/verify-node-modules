using System;

namespace VerifyNodeModules
{
    public class DependencyError
    {
        public DependencyError(string packageName, string expectedVersion, string actualVersion)
        {
            PackageName = packageName ?? throw new ArgumentNullException(nameof(packageName));
            ExpectedVersion = expectedVersion ?? throw new ArgumentNullException(nameof(expectedVersion));
            ActualVersion = actualVersion ?? throw new ArgumentNullException(nameof(actualVersion));
        }

        public string PackageName { get; }
        public string ExpectedVersion { get; }
        public string ActualVersion { get; }
    }
}