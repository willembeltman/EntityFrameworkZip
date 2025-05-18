using EntityFrameworkZip.GeneratedCode;

namespace EntityFrameworkZip.Collections;

internal static class DbContextExtenderCollection
{
    private static readonly Dictionary<Type, object> DbContextExtenders = [];

    internal static DbContextExtender GetOrCreate(DbContext dbContext)
    {
        var type = dbContext.GetType();
        if (DbContextExtenders.TryGetValue(type, out var extender))
        {
            return (DbContextExtender)extender;
        }
        else
        {
            var newExtender = new DbContextExtender(dbContext);
            DbContextExtenders[type] = newExtender;
            return newExtender;
        }
    }
}