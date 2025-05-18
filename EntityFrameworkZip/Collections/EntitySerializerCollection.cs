using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

internal static class EntitySerializerCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntitySerializers = [];

    internal static EntitySerializer<T> GetOrCreate<T>(DbContext dbContext)
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
