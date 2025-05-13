using EntityFrameworkZip.GeneratedCode;
using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip.Collections;

public static class EntityExtenderCollection
{
    private static readonly Dictionary<Type, object> EntityExtenders = new();

    public static EntityExtender<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var serializer = EntityExtenders.ContainsKey(type) ? EntityExtenders[type] : null;
        if (serializer == null)
        {
            var newSerializer = new EntityExtender<T>(dbContext);
            EntityExtenders[type] = newSerializer;
            return newSerializer;
        }
        else
        {
            return (EntityExtender<T>)serializer;
        }
    }
}
