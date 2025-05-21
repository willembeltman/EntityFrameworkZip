namespace EntityFrameworkZip.Navigation;

/// <summary>
/// Implements a lazy-loading foreign entity reference for entities implementing <see cref="IEntity"/>.
/// </summary>
/// <typeparam name="TPrimary">The type of the primary entity that is being referenced.</typeparam>
/// <typeparam name="TForeign">The type of the foreign entity holding the foreign key.</typeparam>
public class LazyEntityNotNull<TPrimary, TForeign> : ILazy<TPrimary?>
    where TPrimary : IEntity
{
    private readonly DbSet<TPrimary> dbSet;
    private readonly TForeign foreign;
    private readonly Func<TForeign, long> getForeignKey;
    private readonly Action<TForeign, long> setForeignKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyEntityNotNull{TPrimary, TForeign}"/> class.
    /// </summary>
    /// <param name="dbSet">The DbSet to query for the primary entity.</param>
    /// <param name="foreign">The foreign entity instance containing the foreign key.</param>
    /// <param name="getForeignKey">Function to get the foreign key value from the foreign entity.</param>
    /// <param name="setForeignKey">Action to set the foreign key value on the foreign entity.</param>
    public LazyEntityNotNull(
        DbSet<TPrimary> dbSet,
        TForeign foreign,
        Func<TForeign, long> getForeignKey,
        Action<TForeign, long> setForeignKey)
    {
        this.dbSet = dbSet;
        this.foreign = foreign;
        this.getForeignKey = getForeignKey;
        this.setForeignKey = setForeignKey;
    }

    /// <summary>
    /// Gets or sets the lazily loaded primary entity.
    /// When getting, it loads the entity from the database if the cached entity is not up-to-date.
    /// When setting, it updates the cached entity and the foreign key on the foreign entity.
    /// If the new value is not yet persisted (Id &lt; 0), it is added to the <see cref="DbSet{T}"/>.
    /// </summary>
    public TPrimary? Value
    {
        get
        {
            var currentForeignKey = getForeignKey(foreign);
            if (currentForeignKey < 1) return default;
            if (!dbSet.ContainsKey(currentForeignKey)) return default;
            return dbSet.FindByKey(currentForeignKey);
        }
        set
        {
            setForeignKey(foreign, value?.Id ?? 0);
        }
    }
}
