namespace EntityFrameworkZip.GeneratedCode;

/// <summary>
/// Provides a cache and factory for <see cref="DbContextFactory"/> instances keyed by DbContext type.
/// </summary>
public static class DbContextFactoryCollection
{
    public static readonly Dictionary<Type, object> DbContextFactorys = [];

    /// <summary>
    /// Gets an existing or creates a new <see cref="DbContextFactory"/> for the specified <see cref="DbContext"/> instance.
    /// </summary>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>A <see cref="DbContextFactory"/> associated with the given <see cref="DbContext"/> type.</returns>
    public static DbContextFactory GetOrCreate(DbContext dbContext)
    {
        var type = dbContext.GetType();
        if (DbContextFactorys.TryGetValue(type, out var extender))
        {
            return (DbContextFactory)extender;
        }
        else
        {
            var newExtender = new DbContextFactory(dbContext);
            DbContextFactorys[type] = newExtender;
            return newExtender;
        }
    }
}