namespace EntityFrameworkZip;

/// <summary>
/// Specifies the foreign key property name for a navigation property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyAttribute"/> class
    /// with the specified foreign key property name.
    /// </summary>
    /// <param name="name">The name of the foreign key property.</param>
    public ForeignKeyAttribute(string name) : base(name)
    {
    }
}