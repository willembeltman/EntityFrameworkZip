namespace EntityFrameworkZip.Tests;

#nullable disable

/// <summary>
/// Represents the rank of a <see cref="Person"/>.
/// </summary>
public enum RankEnum
{
    First,
    Second
}

/// <summary>
/// Represents a person entity that implements <see cref="IEntity"/>.
/// </summary>
public class Person : IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the person.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="Company"/>.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the name of the person.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the rank of the person.
    /// </summary>
    public RankEnum? Rank { get; set; }

    /// <summary>
    /// Gets or sets the lazily loaded reference to the person's company.
    /// Not initialized by default, meaning it will be null until assigned.
    /// </summary>
    public virtual ILazy<Company> Company { get; set; }
}

/// <summary>
/// Represents a company entity that implements <see cref="IEntity"/>.
/// </summary>
public class Company : IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the company.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the owner of the company.
    /// </summary>
    public long? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the name of the company.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the collection of employees associated with the company.
    /// Initialized by default to allow pre-context population.
    /// </summary>
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();

    /// <summary>
    /// Gets or sets the lazily loaded reference to the company's owner.
    /// Initialized using <see cref="LazyStatic{T}"/> for a <see cref="Person"/>.
    /// So you can override the value to reuse an existing person before context insertion.
    /// </summary>
    [ForeignKey("OwnerId")]
    public virtual ILazy<Person> OwnerPerson { get; set; } = new LazyStatic<Person>();

    /// <summary>
    /// Gets or sets a list of temporary to-do items.
    /// This property is ignored by the serializer/database engine due to the <see cref="NotMappedAttribute"/>.
    /// </summary>
    [NotMapped]
    public List<string> TemporaryTodoItems { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the nested financial sub-entity for the company.
    /// Supports nesting of primitive values, lazy references, and collections.
    /// </summary>
    public CompanyFinance Finance { get; set; } = new CompanyFinance();
}

/// <summary>
/// Represents a nested sub-entity containing financial information for a <see cref="Company"/>.
/// </summary>
public class CompanyFinance
{
    /// <summary>
    /// Gets or sets the total revenue.
    /// </summary>
    public decimal? Revenue { get; set; }

    /// <summary>
    /// Gets or sets the total expenses.
    /// </summary>
    public decimal? Expenses { get; set; }

    /// <summary>
    /// Gets the calculated profit, based on revenue minus expenses.
    /// This is a computed property and is not persisted to the database.
    /// </summary>
    public decimal? Profit => Revenue - Expenses;

    /// <summary>
    /// Gets or sets the foreign key to the head of finance.
    /// </summary>
    public long HeadOfFinancePersonId { get; set; }

    /// <summary>
    /// Gets or sets the lazily loaded reference to the head of finance.
    /// Initialized by default with a <see cref="LazyStatic{T}"/> without a value (i.e., null).
    /// The foreign key <see cref="HeadOfFinancePersonId"/> must reside in the same class.
    /// </summary>
    [ForeignKey("HeadOfFinancePersonId")]
    public virtual ILazy<Person> HeadOfFinancePerson { get; set; } = new LazyStatic<Person>();
}

/// <summary>
/// Represents the application's in-memory database context.
/// Uses a Zip-based persistence strategy via <see cref="EntityFrameworkZip.DbContext"/>.
/// </summary>
public class MyDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyDbContext"/> using the given path or filename.
    /// </summary>
    /// <param name="fullName">The full file path or identifier for the underlying Zip file.</param>
    public MyDbContext(string fullName) : base(fullName) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MyDbContext"/> using a directory.
    /// </summary>
    /// <param name="directory">The directory containing the Zip file used for persistence.</param>
    public MyDbContext(DirectoryInfo directory) : base(directory) { }

    /// <summary>
    /// Gets or sets the collection of companies in the context.
    /// Automatically initialized by the framework; manual setup is not required.
    /// </summary>
    public virtual DbSet<Company> Companies { get; set; }

    /// <summary>
    /// Gets or sets the collection of people in the context.
    /// Automatically initialized by the framework; manual setup is not required.
    /// </summary>
    public virtual DbSet<Person> People { get; set; }
}

#nullable enable