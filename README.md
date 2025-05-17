# EntityFrameworkZip

A lightweight, in-memory Entity Framework-style database that saves and loads from a single `.zip` file.

EntityFrameworkZip is designed for scenarios where you want the feel of Entity Framework but without needing a 
real database. It runs entirely in memory and lets you persist your data by zipping everything into a single file. 
Great for testing, demos, prototyping, offline apps, or even small-scale real-world scenarios like save games or 
config snapshots.

---

## ✨ Features

- Familiar `DbContext`-like API
- Use `DbSet<T>` and LINQ to query your entities
- Save and load the entire database from a `.zip` file
- No external dependencies
- Fast and portable
- Easy to integrate into any .NET project

---

## 📦 Installation

Install via NuGet:

```bash

dotnet add package EntityFrameworkZip

```

---

## 🚀 Quick Start (with EF-style syntax)

```csharp

// For simplyfied models
#nullable disable

// A simple Person entity implementing IEntity.
// Includes a reference to a Company via a Lazy<Company> property.
public class Person : IEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; }

    // Lazily loaded reference to the related Company.
    public virtual Lazy<Company> Company { get; set; }
}

// A simple Company entity implementing IEntity.
// Includes a collection of Employees and a Lazy-loaded Owner reference.
public class Company : IEntity
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; }

    // Employees in this company.
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();

    // Lazily loaded reference to the Owner (a Person).
    public virtual Lazy<Person> Owner { get; set; }
}

// The database context. Inherits from the Zip-based EF-like memory database.
public class MyDbContext : DbContext
{
    public MyDbContext(string fullName) : base(fullName) { }

    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Person> People { get; set; }
}

#nulleble enable

// Create or load a database from a zip file.
var db = new MyDbContext("test.zip");

// Create and add Person entities.
var alice = new Person { Name = "Alice" };
db.People.Add(alice);

var bob = new Person { Name = "Bob" };
db.People.Add(bob);

// Create a Company and assign employees *before* adding to the context.
var testCompany = new Company { Name = "Test Company" };
testCompany.Employees.Add(alice);
testCompany.Employees.Add(bob);

// Adding the Company will automatically associate employees via navigation properties.
db.Companies.Add(testCompany);

// Set additional relationship (OwnerId must be explicitly set).
testCompany.OwnerId = alice.Id;

// After being added to the context, lazy loading will resolve relationships.
if (testCompany.Owner.Value.Name != "Alice")
{
    throw new Exception("Test failed: Owner is not Alice.");
}

// Example query: get all people whose name starts with "A".
var all = db.People.Where(p => p.Name.StartsWith("A")).ToList();

// Persist changes to disk (in this case, the zip file).
db.SaveChanges();


```

---

## 🔬 Advanced Example (Navigation, Lazy Loading, Foreign Keys, Nested Sub Entities)


```csharp

using EntityFrameworkZip;
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

#nullable enable

// Create or load the database from a .zip file.
// All operations are performed in-memory; the zip file is only used when SaveChanges() is called.
var db = new MyDbContext("test.zip");

// Create Person entities.
var alice = new Person { Name = "Alice" };
var bob = new Person { Name = "Bob" };

// Add persons to the context.
db.People.Add(alice);
db.People.Add(bob);

// Create a Company and assign employees *before* adding the company to the context.
// Because the Employees list is initialized in the Company class, we can safely add employees here.
var testCompany = new Company { Name = "Test Company" };
testCompany.Employees.Add(alice);
testCompany.Employees.Add(bob);
bob.TestEnum = TestEnum.Second;

// Add the company to the context.
// This will automatically link the previously added Person entities via the navigation property.
db.Companies.Add(testCompany);

// Set the owner relationship explicitly.
// Note: OwnerId must be set manually; the OwnerPerson lazy reference will resolve after adding to the context.
testCompany.OwnerId = alice.Id;

// Verify that lazy loading of the OwnerPerson property works correctly.
if (testCompany.OwnerPerson.Value.Name != "Alice")
{
    throw new Exception("Test failed: Owner is not Alice.");
}

// Query example: select all people whose name starts with "A".
var all = db.People.Where(p => p.CompanyId == testCompany.Id && p.Name.StartsWith("A")).ToList();
if (all.Count != 1)
{
    throw new Exception("Test failed: Expected 1 person, got " + all.Count);
}

// Work with the extended CompanyFinance object.
testCompany.Finance.Revenue = 1000;
testCompany.Finance.Expenses = 500;
testCompany.Finance.HeadOfFinancePersonId = bob.Id;

// Verify calculated property.
if (testCompany.Finance.Profit != 500)
{
    throw new Exception("Test failed: Profit is not 500.");
}

// Verify that lazy loading works inside nested objects.
if (testCompany.Finance.HeadOfFinancePerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Head of Finance is not Bob.");
}

// Verify recursive object resolution via lazy navigation properties.
if (testCompany.Finance.HeadOfFinancePerson.Value.Company.Value.Employees.Count != 2)
{
    throw new Exception("Test failed: Recursive navigation failed.");
}

// Verify recursive read lock
if (!testCompany.Employees.Any(a => a.Company.Value.Employees.Any(b => b.Id == a.Id)))
{
    throw new Exception("Test failed: Recursive read lock failed.");
}

// Persist all changes to disk by saving the entire database to a .zip file.
db.SaveChanges();

var db2 = new MyDbContext("test.zip");
var company2 = db2.Companies.LastOrDefault();
var bob2 = company2.Employees.FirstOrDefault(a => a.Name == "Bob");
if (bob2.TestEnum != TestEnum.Second)
{
    throw new Exception("Test failed: Bob's TestEnum is not Second.");
}

````

---

## 🎯 Use Cases

✅ Unit testing without mocking databases

✅ Desktop apps with lightweight persistence needs

✅ Quick prototypes and demos

✅ Tools that bundle config/data/state in a single zip file

✅ Save game systems or file-based data snapshots

✅ Testing and mocking EF-style data access without a database

✅ Save games or configuration snapshots in games and desktop apps

✅ Portable offline data for single-user apps

✅ Teaching LINQ and EF patterns without needing SQL Server or SQLite

✅ Import/export of structured data in a single zipped file

---
## ❗ Notes

- This is an **in-memory** database — data is only saved if you explicitly call `SaveChanges()`.

- All entities must implement the `IEntity` interface (with at least an `Id` property).

- Foreign key relationships are **automatically managed**.

- **One-to-many** relationships:
  - You can add or remove items directly from the navigation list on the entity.

- **Many-to-one** relationships:
  - These must be set manually via the foreign key `{EntityName}Id` property.
  - The navigation property is read-only and automatically resolved based on the ID — 
    it only needs to be included in the corresponding `DbSet`.


---

## 📄 License
MIT — free to use, modify, and distribute.

---

## 💬 Feedback or Contributions?
Found a bug? Have an idea for a feature?
Feel free to open an issue or contribute via pull request!

---

## 📝 Todo List

- [ ] Implement Attach functionality.

- [ ] Improve support for virtual ICollection<> and IEnumerable<>:
Ensure the insert function updates existing items already in the database, or define a clear behavior for such cases.

- [ ] Investigate using entity proxies via Castle.DynamicProxy:
Still evaluating whether switching to a proxy-based system is worth the added complexity.

- [ ] Add recursive removing of child entities, or at least a exception if we detect foreign keys.

