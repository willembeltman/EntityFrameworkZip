namespace EntityFrameworkZip;

public class Lazy<T>
    where T : IEntity
{
    private readonly DbSet<T> dbSet;
    private readonly object primary;
    private readonly Func<object, long> getCurrentForeignKey;

    public Lazy(
        DbSet<T> dbSet,
        object primary,
        Func<object, long> getCurrentForeignKey)
    {
        this.dbSet = dbSet;
        this.primary = primary;
        this.getCurrentForeignKey = getCurrentForeignKey;
    }

    private T? Cache { get; set; }
    public T? Value
    {
        get
        {
            var currentForeignKey = 
                getCurrentForeignKey(primary);
            if (Cache?.Id != currentForeignKey)
            {
                Cache = dbSet
                    .FirstOrDefault(a => 
                        a.Id == currentForeignKey);
            }
            return Cache;
        }
    }
}
