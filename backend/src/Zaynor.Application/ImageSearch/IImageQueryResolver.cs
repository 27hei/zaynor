namespace Zaynor.Application.ImageSearch;

/// <summary>
/// Turns a photo into a text search query ("what product is this?"), so
/// image search can reuse the exact same aggregation pipeline as typing a
/// query — no separate, unvetted results path.
/// </summary>
public interface IImageQueryResolver
{
    /// <summary>Active only once its provider is configured; otherwise dormant.</summary>
    bool IsEnabled { get; }

    /// <param name="imageUrl">A publicly-fetchable URL for the photo.</param>
    /// <returns>A best-guess product name, or null if nothing was recognized.</returns>
    Task<string?> ResolveQueryAsync(string imageUrl, CancellationToken cancellationToken = default);
}
