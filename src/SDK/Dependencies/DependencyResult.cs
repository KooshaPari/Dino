using System.Collections.Generic;

namespace DINOForge.SDK.Dependencies
{
    public sealed class DependencyResult
    {
        public bool IsSuccess { get; }
        public IReadOnlyList<PackManifest> LoadOrder { get; }
        public IReadOnlyList<string> Errors { get; }

        private DependencyResult(bool isSuccess, IReadOnlyList<PackManifest> loadOrder, IReadOnlyList<string> errors)
        {
            IsSuccess = isSuccess;
            LoadOrder = loadOrder;
            Errors = errors;
        }

        public static DependencyResult Success(IReadOnlyList<PackManifest> loadOrder)
            => new DependencyResult(true, loadOrder, new List<string>().AsReadOnly());

        public static DependencyResult Failure(IReadOnlyList<string> errors)
            => new DependencyResult(false, new List<PackManifest>().AsReadOnly(), errors);
    }
}
