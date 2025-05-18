using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

internal static class EntityExtenderCollection
{
    private static readonly Dictionary<EntityExtenderKey, object> EntityExtenders = [];

    internal static EntityExtender<T> GetOrCreate<T>(DbContext dbContext)
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
