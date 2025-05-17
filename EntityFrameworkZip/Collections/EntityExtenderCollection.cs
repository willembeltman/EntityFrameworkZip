using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

public static class EntityExtenderCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntityExtenders = new();

    public static EntityExtender<T> GetOrCreate<T>(DbContext dbContext)
    {
        var type = typeof(T);
        var applicationDbContextType = dbContext.GetType();
        var search = new EntityExtenderKey(type, applicationDbContextType);
        var serializer = EntityExtenders.ContainsKey(search) ? EntityExtenders[search] : null;
        if (serializer == null)
        {
            var newSerializer = new EntityExtender<T>(dbContext);
            EntityExtenders[search] = newSerializer;
            return newSerializer;
        }
        else
        {
            return (EntityExtender<T>)serializer;
        }
    }
}
