using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

public static class EntitySerializerCollection
{
    private static readonly Dictionary<Type, object> EntitySerializers = new();

    public static EntitySerializer<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityExtenderKey(type, applicationDbContextType);
        var serializer = EntitySerializers.ContainsKey(type) ? EntitySerializers[type] : null;
        if (serializer == null)
        {
            var newSerializer = new EntitySerializer<T>(dbContext);
            EntitySerializers[type] = newSerializer;
            return newSerializer;
        }
        else
        {
            return (EntitySerializer<T>)serializer;
        }
    }
}
