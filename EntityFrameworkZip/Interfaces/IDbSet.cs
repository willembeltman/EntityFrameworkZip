using System.IO.Compression;

namespace EntityFrameworkZip.Interfaces;

/// <summary>
/// Represents a database set that supports caching operations.
/// </summary>
public interface IDbSet
{
    /// <summary>
    /// Gets the type name of the entities in the set.
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Loads the cache from a ZIP archive.
    /// </summary>
    /// <param name="zipArchive">The ZIP archive to load the cache from.</param>
    void LoadCache(ZipArchive zipArchive);

    /// <summary>
    /// Loads the cache from a directory.
    /// </summary>
    /// <param name="directory">The directory to load the cache from.</param>
    void LoadCache(DirectoryInfo directory);

    /// <summary>
    /// Writes the cache to a ZIP archive.
    /// </summary>
    /// <param name="zipArchive">The ZIP archive to write the cache to.</param>
    void WriteCache(ZipArchive zipArchive);

    /// <summary>
    /// Writes the cache to a directory.
    /// </summary>
    /// <param name="directory">The directory to write the cache to.</param>
    void WriteCache(DirectoryInfo directory);
}