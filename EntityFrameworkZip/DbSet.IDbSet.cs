using EntityFrameworkZip.Extentions;
using EntityFrameworkZip.Interfaces;
using System.IO.Compression;

namespace EntityFrameworkZip;

/// <summary>
/// Represents a serializable and extendable set of entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type implementing <see cref="IEntity"/>.</typeparam>
public partial class DbSet<T> : IDbSet
{
    /// <summary>
    /// Gets the type name used as the file prefix for persistence.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Loads the cache from a given <see cref="ZipArchive"/> using .id, .index, and .data entries.
    /// </summary>
    /// <param name="zipArchive">The ZIP archive containing the persisted entity data.</param>
    public void LoadCache(ZipArchive zipArchive)
    {
        Lock.EnterWriteLock();
        try
        {
            var idFile = zipArchive.GetOrCreateEntry($"{TypeName}.id");
            using var idStream = idFile!.Open();
            using var idReader = new BinaryReader(idStream);

            var dataFile = zipArchive.GetOrCreateEntry($"{TypeName}.data");
            using var dataStream = dataFile!.Open();
            using var dataReader = new BinaryReader(dataStream);

            LoadCache(idReader, dataReader);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Loads the cache from files located in a given directory. Files must match the naming pattern {TypeName}.id/.index/.data.
    /// </summary>
    /// <param name="directory">The directory containing the persisted entity files.</param>
    public void LoadCache(DirectoryInfo directory)
    {
        Lock.EnterWriteLock();
        try
        {
            if (!directory.Exists)
                directory.Create();

            var idFullName = Path.Combine(directory.FullName, $"{TypeName}.id");
            using var idStream = File.Open(idFullName, FileMode.OpenOrCreate);
            using var idReader = new BinaryReader(idStream);

            var dataFullName = Path.Combine(directory.FullName, $"{TypeName}.data");
            using var dataStream = File.Open(dataFullName, FileMode.OpenOrCreate);
            using var dataReader = new BinaryReader(dataStream);

            LoadCache(idReader, dataReader);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Writes the entity cache to a given <see cref="ZipArchive"/>, storing .id, .index, and .data entries.
    /// </summary>
    /// <param name="zipArchive">The ZIP archive to write to.</param>
    public void WriteCache(ZipArchive zipArchive)
    {
        Lock.EnterReadLock();
        try
        {
            var idFile = zipArchive.GetOrCreateEntry($"{TypeName}.id");
            using var idStream = idFile!.Open();
            using var idWriter = new BinaryWriter(idStream);

            var dataFile = zipArchive.GetOrCreateEntry($"{TypeName}.data");
            using var dataStream = dataFile!.Open();
            using var dataWriter = new BinaryWriter(dataStream);

            WriteCache(idWriter, dataWriter);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Writes the entity cache to files in the given directory. Overwrites any existing {TypeName}.id/.index/.data files.
    /// </summary>
    /// <param name="directory">The directory to write the files to.</param>
    public void WriteCache(DirectoryInfo directory)
    {
        Lock.EnterReadLock();
        try
        {
            var idFullName = Path.Combine(directory.FullName, $"{TypeName}.id");
            if (File.Exists(idFullName)) File.Delete(idFullName);
            using var idStream = File.Open(idFullName, FileMode.OpenOrCreate);
            using var idWriter = new BinaryWriter(idStream);

            var dataFullName = Path.Combine(directory.FullName, $"{TypeName}.data");
            if (File.Exists(dataFullName)) File.Delete(dataFullName);
            using var dataStream = File.Open(dataFullName, FileMode.OpenOrCreate);
            using var dataWriter = new BinaryWriter(dataStream);

            WriteCache(idWriter, dataWriter);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Reads all entities from the provided streams and rebuilds the cache.
    /// </summary>
    /// <param name="idReader">Reader for the ID file (.id).</param>
    /// <param name="indexReader">Reader for the index file (.index).</param>
    /// <param name="dataStream">Stream for the data file (.data).</param>
    /// <param name="dataReader">Reader for the data stream.</param>
    private void LoadCache(BinaryReader idReader, BinaryReader dataReader)
    {
        if (dataReader.BaseStream.Position < idReader.BaseStream.Length)
            LastId = idReader.ReadInt64();

        while (dataReader.BaseStream.Position < dataReader.BaseStream.Length)
        {
            var item = EntityFactory.Read(dataReader!, DbContext);
            Cache[item.Id] = item;
        }
    }

    /// <summary>
    /// Writes the current entity cache to the given writers.
    /// </summary>
    /// <param name="idWriter">Writer for the ID file.</param>
    /// <param name="indexWriter">Writer for the index file.</param>
    /// <param name="dataWriter">Writer for the data file.</param>
    private void WriteCache(BinaryWriter idWriter, BinaryWriter dataWriter)
    {
        idWriter.Write(LastId);
        foreach (var item in Cache.Values)
        {
            EntityFactory.Write(dataWriter, item, DbContext);
        }
    }
}
