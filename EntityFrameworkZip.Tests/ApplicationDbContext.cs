namespace EntityFrameworkZip.Tests;

#nullable disable

/// <summary>
/// Represents the application's database context, inheriting from EntityFrameworkZip's in-memory Zip-based <see cref="DbContext"/>.
/// Unlike traditional EF contexts, this context operates entirely in memory and does not require <c>SaveChanges()</c>
/// for entity tracking or persistence during normal operation.
///
/// Data is only written to disk when <c>SaveChanges()</c> is explicitly called, which recreates the entire Zip file.
/// This approach works well for small datasets or scenarios where data is saved or loaded occasionally.
/// For large datasets with frequent writes, a different storage model may be more appropriate.
/// </summary>
public class MyDbContext : DbContext
{
    /// <summary>
    /// Constructs a new instance of the database context using the specified file path or name.
    /// This value is used to determine where the Zip file will be stored or loaded from.
    /// </summary>
    /// <param name="fullName">The full path or unique identifier for the Zip storage file.</param>
    public MyDbContext(string fullName) : base(fullName) { }

    /// <summary>
    /// Gets or sets the collection of companies in the database.
    /// No manual initialization is required—handled internally by the engine.
    /// </summary>
    public virtual DbSet<Company> Companies { get; set; }

    /// <summary>
    /// Gets or sets the collection of people in the database.
    /// No manual initialization is required—handled internally by the engine.
    /// </summary>
    public virtual DbSet<Person> People { get; set; }
}
