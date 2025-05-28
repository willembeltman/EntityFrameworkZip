using EntityFrameworkZip.Collections;
using EntityFrameworkZip.GeneratedCode;
using System.IO.Compression;

namespace EntityFrameworkZip;

/// <summary>
/// Represents an in-memory table (DbSet) for entities of type <typeparamref name="T"/>.
/// Supports loading/saving from/to ZIP archives or directories, and manages entity identity.
/// </summary>
/// <typeparam name="T">The entity type. Must implement <see cref="IEntity"/>.</typeparam>
public partial class DbSet<T>
    where T : IEntity
{
    /// <summary>
    /// Provides thread-safe read/write access to the internal cache.
    /// </summary>
    public ReaderWriterLockSlim Lock { get; }

    /// <summary>
    /// The in-memory dictionary storing entities by their ID.
    /// </summary>
    public Dictionary<long, T> Cache { get; }

    /// <summary>
    /// All generated helper functions for the entity of this DbSet.
    /// </summary>
    public EntityFactory<T> EntityFactory { get; }

    /// <summary>
    /// The parent <see cref="DbContext"/> this set belongs to.
    /// </summary>
    public DbContext DbContext { get; }

    /// <summary>
    /// The last used ID for this DbSet. Used to generate unique keys.
    /// </summary>
    public long LastId { get; private set; }

    /// <summary>
    /// Internal constructor used by all public ones. Registers the DbSet with the context,
    /// and initializes serializers, extenders, cache, and locking.
    /// </summary>
    private DbSet(DbContext dbContext)
    {
        DbContext = dbContext;
        DbContext.AddDbSet(this);

        TypeName = typeof(T).Name;
        Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        Cache = new Dictionary<long, T>();
        EntityFactory = EntityFactoryCollection.GetOrCreate<T>(dbContext);
    }

    /// <summary>
    /// Loads the set from a ZIP archive.
    /// </summary>
    /// <param name="dbContext">The context to associate with.</param>
    /// <param name="zipArchive">The archive to read from.</param>
    public DbSet(DbContext dbContext, ZipArchive zipArchive) : this(dbContext)
    {
        LoadCache(zipArchive);
    }

    /// <summary>
    /// Loads the set from a directory.
    /// </summary>
    /// <param name="dbContext">The context to associate with.</param>
    /// <param name="directory">The directory to read from.</param>
    public DbSet(DbContext dbContext, DirectoryInfo directory) : this(dbContext)
    {
        LoadCache(directory);
    }
    
}
