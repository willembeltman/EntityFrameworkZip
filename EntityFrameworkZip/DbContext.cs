using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Interfaces;
using System.IO.Compression;

namespace EntityFrameworkZip;

/// <summary>
/// Represents the main context for managing in-memory data sets and
/// (de)serializing them to and from ZIP files or directories.
/// </summary>
public class DbContext
{
    private string? FullName;
    private DirectoryInfo? Directory;

    /// <summary>
    /// Initializes a new <see cref="DbContext"/> with a ZIP file path.
    /// Automatically loads or creates the archive and populates DbSets.
    /// </summary>
    /// <param name="fullName">The path to the ZIP archive file.</param>
    public DbContext(string fullName)
    {
        FullName = fullName;
        DbSets = [];

        using var ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
        using var ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

        var factory = DbContextFactoryCollection.GetOrCreate(this);
        factory.LoadDbSetsFromZip(this, ZipArchive);
    }

    /// <summary>
    /// Initializes a new <see cref="DbContext"/> from an existing <see cref="ZipArchive"/>.
    /// </summary>
    /// <param name="fullName">The original path of the archive (for saving).</param>
    /// <param name="ZipArchive">The already opened ZIP archive.</param>
    public DbContext(string fullName, ZipArchive ZipArchive)
    {
        FullName = fullName;
        DbSets = [];

        var factory = DbContextFactoryCollection.GetOrCreate(this);
        factory.LoadDbSetsFromZip(this, ZipArchive);
    }

    /// <summary>
    /// Initializes a new <see cref="DbContext"/> using a directory instead of a ZIP file.
    /// All sets will be persisted as files in this directory.
    /// </summary>
    /// <param name="directory">The directory to read from and save to.</param>
    public DbContext(DirectoryInfo directory)
    {
        Directory = directory;
        DbSets = [];

        var factory = DbContextFactoryCollection.GetOrCreate(this);
        factory.LoadDbSetsFromDirectory(this, directory);
    }

    /// <summary>
    /// The registered DbSets belonging to this context.
    /// Internal use only — use AddDbSet to register.
    /// </summary>
    internal List<IDbSet> DbSets;

    /// <summary>
    /// Registers a <see cref="IDbSet"/> with this context so it can be loaded and saved.
    /// </summary>
    /// <param name="dbSet">The DbSet to register.</param>
    public void AddDbSet(IDbSet dbSet)
    {
        DbSets.Add(dbSet);
    }

    /// <summary>
    /// Saves all DbSets to the configured ZIP file or directory.
    /// Will overwrite existing files.
    /// </summary>
    public void SaveChanges()
    {
        if (FullName != null)
        {
            if (File.Exists(FullName))
                File.Delete(FullName);

            using var ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
            using var ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

            foreach (var dbSet in DbSets)
                dbSet.WriteCache(ZipArchive);
        }

        if (Directory != null)
        {
            foreach (var dbSet in DbSets)
                dbSet.WriteCache(Directory);
        }
    }
}
