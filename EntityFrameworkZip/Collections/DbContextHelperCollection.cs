namespace EntityFrameworkZip.Collections;

/// <summary>
/// Provides a cache and factory for <see cref="DbContextHelper"/> instances keyed by DbContext type.
/// </summary>
public static class DbContextHelperCollection
{
    private static readonly Dictionary<Type, object> DbContextHelpers = [];

    /// <summary>
    /// Gets an existing or creates a new <see cref="DbContextHelper"/> for the specified <see cref="DbContext"/> instance.
    /// </summary>
    /// <param name="dbContext">The database context instance.</param>
    /// <returns>A <see cref="DbContextHelper"/> associated with the given <see cref="DbContext"/> type.</returns>
    public static DbContextHelper GetOrCreate(DbContext dbContext)
    {
        var type = dbContext.GetType();
        if (DbContextHelpers.TryGetValue(type, out var extender))
        {
            return (DbContextHelper)extender;
        }
        else
        {
            var newExtender = new DbContextHelper(dbContext);
            DbContextHelpers[type] = newExtender;
            return newExtender;
        }
    }
}