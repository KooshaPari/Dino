using System.Collections.Generic;

namespace DINOForge.SDK
{
    /// <summary>
    /// Immutable result of a content loading operation.
    /// </summary>
    public sealed class ContentLoadResult
    {
        /// <summary>Whether all content loaded without errors.</summary>
        public bool IsSuccess { get; }

        /// <summary>IDs of packs that were successfully loaded.</summary>
        public IReadOnlyList<string> LoadedPacks { get; }

        /// <summary>All errors encountered during loading.</summary>
        public IReadOnlyList<string> Errors { get; }

        private ContentLoadResult(bool isSuccess, IReadOnlyList<string> loadedPacks, IReadOnlyList<string> errors)
        {
            IsSuccess = isSuccess;
            LoadedPacks = loadedPacks;
            Errors = errors;
        }

        /// <summary>Creates a successful result.</summary>
        public static ContentLoadResult Success(IReadOnlyList<string> loadedPacks)
            => new ContentLoadResult(true, loadedPacks, new List<string>().AsReadOnly());

        /// <summary>Creates a failed result.</summary>
        public static ContentLoadResult Failure(IReadOnlyList<string> errors)
            => new ContentLoadResult(false, new List<string>().AsReadOnly(), errors);

        /// <summary>Creates a partial result: some packs loaded, but errors occurred.</summary>
        public static ContentLoadResult Partial(IReadOnlyList<string> loadedPacks, IReadOnlyList<string> errors)
            => new ContentLoadResult(false, loadedPacks, errors);
    }
}
