using System.Collections;

namespace EntityFrameworkZip.Extended;

/// <summary>
/// PLEASE DO NOT USE THIS AS INITIALIZATION FOR A ICOLLECTION OR IENUMERABLE PROPERTY OF YOUR ENTITY.
/// This class is only public because it is used in the generated code.
/// The ZipDatabase engine actively looks if this class is used (aka the generated code made it) 
/// to determine if it should try to add the items inside of the collection to the database.
/// If it is used, it will NOT try to add, if it isn't used (like when you simple write new List<>())
/// it WILL try to add the items inside of the collection to the database because it assumes the items
/// should be new. Adding the items will cause the id's to be set.
/// </summary>
public class LazyForeignEntityCollection<TForeign, TPrimary> : ICollection<TForeign>
    where TForeign : IEntity
    where TPrimary : IEntity
{
    private readonly DbSet<TForeign> dbSet;
    private readonly TPrimary primary;
    private readonly Func<TForeign, TPrimary, bool> whereForeignKeyEqualToPrimaryKeyFunction;
    private readonly Action<TForeign, TPrimary> setForeignKeyToPrimaryKeyFunction;

    public LazyForeignEntityCollection(
        DbSet<TForeign> dbSet,
        TPrimary primary,
        Func<TForeign, TPrimary, bool> whereForeignKeyEqualToPrimaryKeyFunction,
        Action<TForeign, TPrimary> setForeignKeyToPrimaryKeyFunction)
    {
        this.dbSet = dbSet;
        this.primary = primary;
        this.whereForeignKeyEqualToPrimaryKeyFunction = whereForeignKeyEqualToPrimaryKeyFunction;
        this.setForeignKeyToPrimaryKeyFunction = setForeignKeyToPrimaryKeyFunction;
    }

    public int Count => dbSet.Count(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary));
    public bool IsReadOnly => false;

    public void Add(TForeign item)
    {
        setForeignKeyToPrimaryKeyFunction(item, primary);
        dbSet.Add(item);
    }

    public void CopyTo(TForeign[] array, int arrayIndex)
    {
        foreach (var item in dbSet.Where(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary)))
        {
            if (arrayIndex >= array.Length) throw new ArgumentException("Target array too small");
            array[arrayIndex++] = item;
        }
    }
    public bool Remove(TForeign item)
    {
        if (!dbSet.Any(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary) && a.Id == item.Id)) return false;
        return dbSet.Remove(item);
    }

    public void Clear()
    {
        var list = dbSet.Where(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary)).ToArray();
        dbSet.RemoveRange(list);
    }

    public bool Contains(TForeign item)
    {
        return dbSet.Any(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary) && a.Id == item.Id);
    }

    public IEnumerator<TForeign> GetEnumerator()
    {
        foreach (var item in dbSet.Where(a => whereForeignKeyEqualToPrimaryKeyFunction(a, primary)))
        {
            yield return item;
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}