namespace Zaynor.Application.ImageSearch;

/// <summary>
/// Short-lived, in-memory storage for an uploaded photo, so it can be handed
/// a public URL long enough for Google's own crawler to fetch it (the
/// reverse-image lookup API takes a URL, not raw bytes). Never meant as
/// durable storage — entries expire quickly.
/// </summary>
public interface ITempImageStore
{
    /// <summary>Stores the bytes and returns an opaque id to fetch them back by.</summary>
    string Save(byte[] bytes, string contentType);

    /// <summary>Returns the stored bytes/content-type, or null if missing/expired.</summary>
    (byte[] Bytes, string ContentType)? Get(string id);
}
