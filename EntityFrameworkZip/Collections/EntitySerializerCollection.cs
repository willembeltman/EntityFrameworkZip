using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="EntitySerializer{T}"/> instances keyed by entity type and DbContext type.
/// </summary>
public static class EntitySerializerCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntitySerializers = [];
    
    /// <summary>
    /// Gets an existing or creates a new <see cref="EntitySerializer{T}"/> for the specified entity type and <see cref="DbContext"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>An <see cref="EntitySerializer{T}"/> associated with the given entity and context types.</returns>
    public static EntitySerializer<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityExtenderKey(type, applicationDbContextType);
        if (EntitySerializers.TryGetValue(search, out var serializer))
        {
            return (EntitySerializer<T>)serializer;
        }
        else
        {
            var newSerializer = new EntitySerializer<T>(dbContext);
            EntitySerializers[search] = newSerializer;
            return newSerializer;
        }
    }
}
