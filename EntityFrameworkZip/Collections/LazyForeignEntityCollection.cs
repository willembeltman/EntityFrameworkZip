using System.Collections;
using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip.Collections;

public class LazyForeignEntityCollection<TForeign, TPrimary> : ICollection<TForeign>
    where TForeign : IEntity
    where TPrimary : IEntity
{
    private readonly DbSet<TForeign> DbSet;
    private readonly Func<TForeign, TPrimary, bool> WhereForeignKeyEqualToPrimaryKeyFunction;
    private readonly Action<TForeign, TPrimary> SetForeignKeyToPrimaryKeyFunction;
    private readonly TPrimary Primary;

    /// <summary>
    /// PLEASE DO NOT USE THIS AS INITIALIZATION FOR A ICOLLECTION OR IENUMERABLE PROPERTY OF YOUR ENTITY.
    /// This class is only public because it is used in the generated code.
    /// The ZipDatabase engine actively looks if this class is used (aka the generated code made it) 
    /// to determine if it should try to add the items inside of the collection to the database.
    /// If it is used, it will NOT try to add, if it isn't used (like when you simple write new List<>())
    /// it WILL try to add the items inside of the collection to the database because it assumes the items
    /// should be new. Adding the items will cause the id's to be set.
    /// </summary>
    public LazyForeignEntityCollection(
        DbSet<TForeign> dbSet, 
        TPrimary primary, 
        Func<TForeign, TPrimary, bool> whereForeignKeyEqualToPrimaryKeyFunction, 
        Action<TForeign, TPrimary> setForeignKeyToPrimaryKeyFunction)
    {
        DbSet = dbSet;
        WhereForeignKeyEqualToPrimaryKeyFunction = whereForeignKeyEqualToPrimaryKeyFunction;
        SetForeignKeyToPrimaryKeyFunction = setForeignKeyToPrimaryKeyFunction;
        Primary = primary;
    }

    public int Count => DbSet.Count(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary));
    public bool IsReadOnly => false;

    public void Add(TForeign item)
    {
        SetForeignKeyToPrimaryKeyFunction(item, Primary);
        DbSet.Add(item);
    }

    public void CopyTo(TForeign[] array, int arrayIndex)
    {
        foreach (var item in DbSet.Where(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary)))
        {
            if (arrayIndex >= array.Length) throw new ArgumentException("Target array too small");
            array[arrayIndex++] = item;
        }
    }
    public bool Remove(TForeign item)
    {
        if (!DbSet.Any(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary) && a.Id == item.Id)) return false;
        return DbSet.Remove(item);
    }

    public void Clear()
    {
        var list = DbSet.Where(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary)).ToArray();
        DbSet.RemoveRange(list);
    }

    public bool Contains(TForeign item)
    {
        return DbSet.Any(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary) && a.Id == item.Id);
    }

    public IEnumerator<TForeign> GetEnumerator()
    {
        foreach (var item in DbSet.Where(a => WhereForeignKeyEqualToPrimaryKeyFunction(a, Primary)))
        {
            yield return item;
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}