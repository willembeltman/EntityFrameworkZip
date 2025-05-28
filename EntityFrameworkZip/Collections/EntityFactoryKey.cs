namespace EntityFrameworkZip.Collections;

/// <summary>
/// Key class used to lookup cached EntityFactories
/// </summary>
public readonly struct EntityFactoryKey : IEquatable<EntityFactoryKey>
{
    /// <summary>
    /// Constructor for key class used by EntityFactory cache
    /// </summary>
    /// <param name="entityType">The </param>
    /// <param name="applicationDbContextType"></param>
    public EntityFactoryKey(Type entityType, Type applicationDbContextType) : this()
    {
        EntityType = entityType;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type EntityType { get; }
    public Type ApplicationDbContextType { get; }

    /// <summary>
    /// IEquatable interface for lookup during Cache
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(EntityFactoryKey other)
        => EntityType == other.EntityType &&
           ApplicationDbContextType == other.ApplicationDbContextType;
}
