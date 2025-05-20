namespace EntityFrameworkZip;

/// <summary>
/// Indicates that a property is not mapped to a database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotMappedAttribute : Attribute { }
