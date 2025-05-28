using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="EntityFactory{T}"/> instances keyed by entity type and DbContext type.
/// </summary>
public static class EntityFactoryCollection
{
    public static readonly Dictionary<EntityFactoryKey, object> EntityFactorys = [];

    /// <summary>
    /// Gets an existing or creates a new <see cref="EntityFactory{T}"/> for the specified entity type and <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>An <see cref="EntityFactory{T}"/> associated with the given entity and context types.</returns>
    public static EntityFactory<T> GetOrCreate<T>(DbContext dbContext)
    {
        var entityType = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityFactoryKey(entityType, applicationDbContextType);
        if (EntityFactorys.TryGetValue(search, out var entityFactory))
        {
            return (EntityFactory<T>)entityFactory;
        }
        else
        {
            var newEntityFactory = new EntityFactory<T>(dbContext);
            EntityFactorys[search] = newEntityFactory;
            return newEntityFactory;
        }
    }
}
