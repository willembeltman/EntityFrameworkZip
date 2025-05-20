using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="ForeignKeyUsageFinder{T}"/> instances
/// keyed by entity type and DbContext type.
/// </summary>
public static class ForeignKeyUsageFinderCollection
{
    private static readonly Dictionary<ForeignKeyUsageFinderKey, object> ForeignKeyUsageFinders = [];
   
    /// <summary>
    /// Gets an existing or creates a new <see cref="ForeignKeyUsageFinder{T}"/> for the specified entity type and DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>An <see cref="ForeignKeyUsageFinder{T}"/> associated with the given entity type and DbContext type.</returns>
    public static ForeignKeyUsageFinder<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new ForeignKeyUsageFinderKey(type, applicationDbContextType);
        if (ForeignKeyUsageFinders.TryGetValue(search, out var extender))
        {
            return (ForeignKeyUsageFinder<T>)extender;
        }
        else
        {
            var newExtender = new ForeignKeyUsageFinder<T>(dbContext);
            ForeignKeyUsageFinders[search] = newExtender;
            return newExtender;
        }
    }
}
