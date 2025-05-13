using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

public static class DbContextExtenderCollection
{
    private static readonly Dictionary<Type, object> DbContextExtenders = new();

    public static DbContextExtender GetOrCreate(DbContext dbContext)
    {
        var type = dbContext.GetType();
        var serializer = DbContextExtenders.ContainsKey(type) ? DbContextExtenders[type] : null;
        if (serializer == null)
        {
            var newSerializer = new DbContextExtender(dbContext);
            DbContextExtenders[type] = newSerializer;
            return newSerializer;
        }
        else
        {
            return (DbContextExtender)serializer;
        }
    }
}