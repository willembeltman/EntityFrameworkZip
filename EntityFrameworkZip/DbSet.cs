using System.Collections;
using System.IO.Compression;
using EntityFrameworkZip.Interfaces;
using EntityFrameworkZip.Extentions;
using EntityFrameworkZip.GeneratedCode;
using EntityFrameworkZip.Collections;

namespace EntityFrameworkZip;

public class DbSet<T> : ICollection<T>, IDbSet
    where T : IEntity
{
    private readonly ReaderWriterLockSlim Lock;
    private readonly Dictionary<long, T> Cache;
    private readonly EntitySerializer<T> EntitySerializer;
    private readonly EntityExtender<T> EntityExtender;
    private long LastId;

    public DbContext DbContext { get; }
    public string TypeName { get; }

    public DbSet(DbContext dbContext, ZipArchive zipArchive)
    {
        DbContext = dbContext;
        DbContext.AddDbSet(this);

        TypeName = typeof(T).Name;
        Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        Cache = new Dictionary<long, T>();
        EntitySerializer = EntitySerializerCollection.GetOrCreate<T>(dbContext);
        EntityExtender = EntityExtenderCollection.GetOrCreate<T>(dbContext);

        LoadCache(zipArchive);
    }

    public void LoadCache(ZipArchive zipArchive)
    {
        Lock.EnterWriteLock();
        try
        {
            var idFile = zipArchive.GetOrCreateEntry($"{TypeName}.id");
            using var idStream = idFile!.Open();
            using var idReader = new BinaryReader(idStream);

            var indexFile = zipArchive.GetOrCreateEntry($"{TypeName}.index");
            using var indexStream = indexFile!.Open();
            using var indexReader = new BinaryReader(indexStream);

            var dataFile = zipArchive.GetOrCreateEntry($"{TypeName}.data");
            using var dataStream = dataFile!.Open();
            using var dataReader = new BinaryReader(dataStream);

            if (idStream.Position < idStream.Length)
                LastId = idReader.ReadInt64();

            while (indexStream!.Position < indexStream.Length)
            {
                var indexPosition = indexStream.Position;
                var dataPosition = indexReader!.ReadInt64();
                if (dataPosition >= 0)
                {
                    dataStream.Position = dataPosition;
                    var item = EntitySerializer.Read(dataReader!, DbContext);
                    //EntityExtender.ExtendEntity(item, DbContext);
                    Cache[item.Id] = item;
                }
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void WriteCache(ZipArchive zipArchive)
    {
        Lock.EnterReadLock();
        try
        {
            var idFile = zipArchive.GetOrCreateEntry($"{TypeName}.id");
            using var idStream = idFile!.Open();
            using var idWriter = new BinaryWriter(idStream);

            var indexFile = zipArchive.GetOrCreateEntry($"{TypeName}.index");
            using var indexStream = indexFile!.Open();
            using var indexWriter = new BinaryWriter(indexStream);

            var dataFile = zipArchive.GetOrCreateEntry($"{TypeName}.data");
            using var dataStream = dataFile!.Open();
            using var dataWriter = new BinaryWriter(dataStream);

            idWriter.Write(LastId);
            foreach (var item in Cache.Values)
            {
                indexWriter.Write(dataStream.Position);
                EntitySerializer.Write(dataWriter, item, DbContext);
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    public void Attach(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        if (item.Id < 1)
        {
            Add(item);
            return;
        }

        Lock.EnterWriteLock();
        try
        {
            Cache[item.Id] = item;
            EntityExtender.ExtendEntity(item, DbContext);
            if (LastId < item.Id) LastId = item.Id;
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void ExtendEntity(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        EntityExtender.ExtendEntity(item, DbContext);
    }

    #region ICollection
    public int Count
    {
        get
        {
            Lock.EnterReadLock();
            try { return Cache.Count; }
            finally { Lock.ExitReadLock(); }
        }
    }
    public bool IsReadOnly => false;

    public void Add(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (Contains(item)) throw new ArgumentException("Item already exists in the collection");

        Lock.EnterWriteLock();
        try
        {
            item.Id = ++LastId;
            Cache[item.Id] = item;
            EntityExtender.ExtendEntity(item, DbContext);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool Remove(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        Lock.EnterWriteLock();
        try
        {
            if (!Cache.TryGetValue(item.Id, out var entry)) return false;
            Cache.Remove(item.Id);
            return true;
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public int RemoveRange(IEnumerable<T> enumerable)
    {
        int count = 0;

        Lock.EnterWriteLock();
        try
        {
            foreach (var item in enumerable)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                if (!Cache.TryGetValue(item.Id, out var entry))
                {
                    continue;
                }
                Cache.Remove(item.Id);
                count++;
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }

        return count;
    }
    public void Clear()
    {
        Lock.EnterWriteLock();
        try
        {
            Cache.Clear();
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool Contains(T item)
    {
        if (item == null) return false;

        Lock.EnterReadLock();
        try
        {
            return Cache.ContainsKey(item.Id);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public void CopyTo(T[] array, int arrayIndex)
    {
        Lock.EnterReadLock();
        try
        {
            foreach (var item in Cache.Values)
            {
                //EntityExtender.ExtendEntity(item, DbContext);
                if (arrayIndex >= array.Length) throw new ArgumentException("Target array too small");
                array[arrayIndex++] = item;
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public IEnumerator<T> GetEnumerator()
    {
        Lock.EnterReadLock();
        try
        {
            foreach (var item in Cache.Values)
            {
                EntityExtender.ExtendEntity(item, DbContext);
                yield return item;
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
