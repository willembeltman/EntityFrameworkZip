using EntityFrameworkZip.Attributes;
using EntityFrameworkZip.Interfaces;

#nullable disable

public enum TestEnum
{
    First,
    Second,
    Third
}

/// <summary>
/// Represents a person entity that implements <see cref="IEntity"/>.
/// Contains a lazy-loaded reference to a related <see cref="Company"/>.
/// </summary>
public class Person : IEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; }
    public TestEnum TestEnum { get; set; } 

    /// <summary>
    /// Lazily loaded reference to the company this person belongs to.
    /// </summary>
    public virtual Lazy<Company> Company { get; set; }
}

/// <summary>
/// Represents a company entity that implements <see cref="IEntity"/>.
/// Contains a collection of employees, a lazy-loaded owner reference, and a nested sub-entity for financial data.
/// </summary>
public class Company : IEntity
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// A collection of employees (type <see cref="Person"/>).
    /// Initialized by default, allowing employees to be added before the company is added to the context.
    /// Once the company is added to the context, all employees in this list will also be added.
    /// </summary>
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();

    /// <summary>
    /// Lazily loaded reference to the owner of the company.
    /// The <see cref="ForeignKeyAttribute"/> specifies that the foreign key is <see cref="OwnerId"/>.
    /// </summary>
    [ForeignKey("OwnerId")]
    public virtual Lazy<Person> OwnerPerson { get; set; }

    /// <summary>
    /// A temporary list of to-do items. This property is not mapped to the database.
    /// Any type is supported here, as long as it's marked with <see cref="NotMappedAttribute"/>.
    /// </summary>
    [NotMapped]
    public List<string> TemporaryTodoItems { get; set; } = new List<string>();

    /// <summary>
    /// A nested sub-entity holding financial information for the company.
    /// Sub-entities follow the same rules as regular entities and can contain:
    /// - Primitive types
    /// - <see cref="DateTime"/> values
    /// - Lazy-loaded navigation properties
    /// - Collections (e.g., <see cref="ICollection{T}"/>)
    /// - Other sub-entities
    /// </summary>
    public CompanyFinance Finance { get; set; } = new CompanyFinance();
}

/// <summary>
/// Represents financial data associated with a <see cref="Company"/>.
/// Demonstrates support for nested sub-entities, foreign keys, and computed properties.
/// </summary>
public class CompanyFinance
{
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }

    /// <summary>
    /// Computed profit based on revenue and expenses.
    /// Read-only properties are automatically ignored by the ORM and do not require [NotMapped].
    /// </summary>
    public decimal Profit => Revenue - Expenses;

    public long HeadOfFinancePersonId { get; set; }

    /// <summary>
    /// Lazily loaded reference to the head of finance.
    /// The <see cref="ForeignKeyAttribute"/> is used to map to <see cref="HeadOfFinancePersonId"/>.
    /// This must be defined within the same class, as parent classes have no visibility into the sub-entity’s structure.
    /// </summary>
    [ForeignKey("HeadOfFinancePersonId")]
    public virtual Lazy<Person> HeadOfFinancePerson { get; set; }
}
