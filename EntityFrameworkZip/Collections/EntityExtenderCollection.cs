using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="EntityExtender{T}"/> instances
/// keyed by entity type and DbContext type.
/// </summary>
public static class EntityExtenderCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntityExtenders = [];
   
    /// <summary>
    /// Gets an existing or creates a new <see cref="EntityExtender{T}"/> for the specified entity type and DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>An <see cref="EntityExtender{T}"/> associated with the given entity type and DbContext type.</returns>
    public static EntityExtender<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityExtenderKey(type, applicationDbContextType);
        if (EntityExtenders.TryGetValue(search, out var extender))
        {
            return (EntityExtender<T>)extender;
        }
        else
        {
            var newExtender = new EntityExtender<T>(dbContext);
            EntityExtenders[search] = newExtender;
            return newExtender;
        }
    }
}
