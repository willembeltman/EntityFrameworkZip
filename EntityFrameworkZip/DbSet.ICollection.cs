using System.Collections;

namespace EntityFrameworkZip;

public partial class DbSet<T> : ICollection<T>
{
    /// <summary>
    /// Gets the number of entities currently in the collection.
    /// </summary>
    public int Count
    {
        get
        {
            Lock.EnterReadLock();
            try { return Cache.Count; }
            finally { Lock.ExitReadLock(); }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// Always returns false, since items can be added or removed.
    /// </summary>
    public bool IsReadOnly => false;
    
    /// <summary>
    /// Adds a new entity to the collection. 
    /// Automatically assigns a new ID and extends the entity with the current DbContext.
    /// Throws an exception if the item is null or already exists.
    /// </summary>
    /// <param name="item">The entity to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the item already exists.</exception>
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

    /// <summary>
    /// Adds or replaces an entity in the cache. If it has no ID yet, assigns one.
    /// Also extends its navigation properties.
    /// </summary>
    /// <param name="item">The entity to attach.</param>
    public void Attach(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        // If it's a new entity (uninitialized ID), treat it as an Add.
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

            // Track the highest ID so new entities get a unique one.
            if (LastId < item.Id) LastId = item.Id;
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Retrieves an entity based on its primary key ID.
    /// Returns the corresponding entity if it exists, or <c>null</c> if the ID is not found in the cache.
    /// Thread-safe via a read lock.
    /// </summary>
    /// <param name="id">The primary key ID of the entity.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public T? FindByKey(long id)
    {
        Lock.EnterReadLock();
        try
        {
            return Cache[id];
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    
    /// <summary>
    /// Checks whether an entity with the specified primary key ID exists in the cache.
    /// Thread-safe via a read lock.
    /// </summary>
    /// <param name="id">The primary key ID to check.</param>
    /// <returns><c>true</c> if the ID exists in the cache; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(long id)
    {
        Lock.EnterReadLock();
        try
        {
            return Cache.ContainsKey(id);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes the specified entity from the collection if it exists.
    /// </summary>
    /// <param name="item">The entity to remove.</param>
    /// <returns>True if the entity was found and removed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
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

    /// <summary>
    /// Removes a range of entities from the collection.
    /// Each entity is looked up by ID and removed if present.
    /// </summary>
    /// <param name="enumerable">The sequence of entities to remove.</param>
    /// <returns>The number of entities successfully removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an item in the sequence is null.</exception>
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

    /// <summary>
    /// Clears all entities from the collection.
    /// </summary>
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

    /// <summary>
    /// Determines whether the specified entity exists in the collection by ID.
    /// </summary>
    /// <param name="item">The entity to locate.</param>
    /// <returns>True if the entity exists in the collection; otherwise, false.</returns>
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

    /// <summary>
    /// Copies the entities from the collection to the specified array starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    /// <exception cref="ArgumentException">Thrown if the destination array is too small.</exception>
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

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// Extends each entity with the current DbContext before yielding.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
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

    /// <summary>
    /// Returns an enumerator that iterates through the collection (non-generic).
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
