using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="EntityHelper{T}"/> instances keyed by entity type and DbContext type.
/// </summary>
public static class EntityHelperCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntityHelpers = [];
    
    /// <summary>
    /// Gets an existing or creates a new <see cref="EntityHelper{T}"/> for the specified entity type and <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>An <see cref="EntityHelper{T}"/> associated with the given entity and context types.</returns>
    public static EntityHelper<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityExtenderKey(type, applicationDbContextType);
        if (EntityHelpers.TryGetValue(search, out var serializer))
        {
            return (EntityHelper<T>)serializer;
        }
        else
        {
            var newSerializer = new EntityHelper<T>(dbContext);
            EntityHelpers[search] = newSerializer;
            return newSerializer;
        }
    }
}
