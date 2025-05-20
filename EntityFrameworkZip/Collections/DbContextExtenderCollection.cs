namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="DbContextExtender"/> instances keyed by DbContext type.
/// </summary>
public static class DbContextExtenderCollection
{
    private static readonly Dictionary<Type, object> DbContextExtenders = [];

    /// <summary>
    /// Gets an existing or creates a new <see cref="DbContextExtender"/> for the specified <see cref="DbContext"/> instance.
    /// </summary>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>A <see cref="DbContextExtender"/> associated with the given <see cref="DbContext"/> type.</returns>
    public static DbContextExtender GetOrCreate(DbContext dbContext)
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