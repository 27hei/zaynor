using System.Runtime.CompilerServices;

// Lets tests observe internal-only hooks (e.g. CachedAggregationService's
// fire-and-forget background task) without exposing them on the public API.
[assembly: InternalsVisibleTo("Zaynor.Application.Tests")]
