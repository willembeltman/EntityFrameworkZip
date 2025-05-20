namespace EntityFrameworkZip;

/// <summary>
/// Represents an entity with a unique identifier.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// </summary>
    long Id { get; set; }
}