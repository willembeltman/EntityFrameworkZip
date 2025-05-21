# EntityFrameworkZip

A lightweight, in-memory Entity Framework-style database that saves and loads from a single `.zip` file.

EntityFrameworkZip is designed for scenarios where you want the feel of Entity Framework but without needing a real database. It runs entirely in memory and lets you persist your data by zipping everything into a single file. Great for testing, demos, prototyping, offline apps, or even small-scale real-world scenarios like save games or config snapshots.

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
// Includes a reference to a Company via a ILazy<Company> property.
public class Person : IEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; }

    // Lazily loaded reference to the related Company.
    public virtual ILazy<Company> Company { get; set; }
}

// A simple Company entity implementing IEntity.
// Includes a collection of Employees and a ILazy-loaded Owner reference.
public class Company : IEntity
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; }

    // Employees in this company.
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();

    // Lazily loaded reference to the Owner (a Person).
    public virtual ILazy<Person> Owner { get; set; }
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
    public long CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the name of the person.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the rank of the person.
    /// </summary>
    public RankEnum Rank { get; set; }

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
    public long OwnerId { get; set; }

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
    public decimal Revenue { get; set; }

    /// <summary>
    /// Gets or sets the total expenses.
    /// </summary>
    public decimal Expenses { get; set; }

    /// <summary>
    /// Gets the calculated profit, based on revenue minus expenses.
    /// This is a computed property and is not persisted to the database.
    /// </summary>
    public decimal Profit => Revenue - Expenses;

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

// Example test application demonstrating usage of the EntityFrameworkZip in-memory database.
// Verifies lazy loading, entity relationships, nested entity behavior, and persistence.
#nullable enable

// Create or load the database from a .zip file.
// All operations are performed in-memory; the zip file is only used when SaveChanges() is called.
var db = new MyDbContext(new DirectoryInfo("test")); // => Using a directory instead of zip for speed.

// Create Person entities representing employees or owners.
var alice = new Person { Name = "Alice", Rank = RankEnum.First };
var bob = new Person { Name = "Bob", Rank = RankEnum.Second };

// Create a Company and assign employees before adding it to the context.
// Since Employees is initialized by default, we can safely add people before persistence.
var testCompany = new Company { Name = "Test Company" };
testCompany.OwnerPerson.Value = alice;
testCompany.Employees.Add(alice);
testCompany.Employees.Add(bob);

// Add the company to the context. This operation also registers all related employees.
db.Companies.Add(testCompany);

// Verify initial lazy loading of the OwnerPerson property.
if (testCompany.OwnerPerson.Value.Name != "Alice")
{
    throw new Exception("Test failed: Owner is not Alice.");
}

// Change the owner to Bob and verify the lazy-loaded relationship updates correctly.
testCompany.OwnerPerson.Value = bob;
if (testCompany.OwnerPerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Owner is not Bob.");
}

// Perform a LINQ query to find all employees whose name starts with 'A'.
// Ensures filtering and foreign key resolution works.
var all = db.People.Where(p => p.CompanyId == testCompany.Id && p.Name.StartsWith('A')).ToList();
if (all.Count != 1)
{
    throw new Exception("Test failed: Expected 1 person, got " + all.Count);
}

// Set financial data and validate computed values.
testCompany.Finance.Revenue = 1000;
testCompany.Finance.Expenses = 500;

// Verify calculated Profit property reflects the correct value.
if (testCompany.Finance.Profit != 500)
{
    throw new Exception("Test failed: Profit is not 500.");
}

// Set HeadOfFinance to bob by id
testCompany.Finance.HeadOfFinancePersonId = bob.Id;

// Verify lazy loading inside nested sub-entities (Finance).
if (testCompany.Finance.HeadOfFinancePerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Head of Finance is not Bob.");
}

// Verify recursive resolution of lazy navigation properties.
// Ensures nested references back to parent entities are correctly resolved.
if (testCompany.Finance.HeadOfFinancePerson.Value.Company.Value.Employees.Count != 2)
{
    throw new Exception("Test failed: Recursive navigation failed.");
}

// Verify recursive relationship lookup through lazy references.
// Ensures consistency in navigation and lazy loading.
if (!testCompany.Employees.Any(a => a.Company.Value.Employees.Any(b => b.Id == a.Id)))
{
    throw new Exception("Test failed: Recursive read lock failed.");
}

// Persist all changes by saving the in-memory state to a .zip file.
db.SaveChanges();

// Reload the database from the saved .zip file to verify persistence.
var db2 = new MyDbContext(new DirectoryInfo("test"));
var company2 = db2.Companies.Last();
var bob2 = company2.Employees.First(a => a.Name == "Bob");

// Verify deserialized entity properties, including enums and lazy relationships.
if (bob2.Rank != RankEnum.Second)
{
    throw new Exception("Test failed: Bob's RankEnum is not Second.");
}
if (company2.OwnerPerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Owner is not Bob after reload.");
}

```

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

## ⚠️ Breaking Change in Lazy Navigation Properties

As of version 1.0.8, Lazy<T> has been replaced with a new interface: ILazy<T>.

Why the change?
Previously, the internal Lazy<T> class would cache the related entity after the first lookup. If you later changed the foreign key ({EntityName}Id), the cached object would still point to the old value — resulting in stale data and hard-to-find bugs.

To fix this, the new ILazy<T> interface allows dynamic reloading. It ensures that each time you access the navigation property, it re-checks the current foreign key and fetches the correct related entity accordingly.

What do I need to change?
Simply update your properties or fields:

// Before:
```csharp
public Lazy<Person> Owner;
```

// After:
```csharp
public ILazy<Person> Owner;
```

Sorry for the inconvenience.

We try to keep the interface stable, but this change was necessary to prevent subtle data bugs in apps that modify foreign key references at runtime.

If you run into issues updating your code, feel free to open an issue — we're happy to help.

---

## 📄 License

MIT — free to use, modify, and distribute.

---

## 💬 Feedback or Contributions?

Found a bug? Have an idea for a feature?
Feel free to open an issue or contribute via pull request!

---

## 📝 Todo List

- [X] Implement Attach functionality. (Done in 1.0.8)

- [X] Improve support for virtual ICollection<> and IEnumerable<>:
Ensure the insert function updates existing items already in the database, or define a clear behavior for such cases.
(Done in 1.0.10)

- [X] Investigate using entity proxies via Castle.DynamicProxy:
Still evaluating whether switching to a proxy-based system is worth the added complexity.
Conclusion: not going to do it.

- [X] Add recursive removing of child entities, or at least a exception if we detect foreign keys.
(Done in 1.0.10)


---

## 📝 Changelog

### v1.0.10
- Adjusted ICollection.Remove behavior: now only unsets the foreign key (sets it to 0 or null) instead of removing the referenced entity from the DbSet<TForeign>.
- Standardized behavior for assigning related entities to new entities to ensure correct add/update semantics.
- Added support for nullable properties (surprisingly, this was previously unsupported — oops!).
- Enabled support for nullable foreign keys (long?), which now default to null instead of 0.
- Updated the test program to include tests for nullable fields.
- Consolidated all generated code to improve startup performance.
- Performed extensive code cleanup.
- Implemented detection of foreign key usage when removing an entity. An exception is now thrown if the foreign key is still in use. You can override this behavior to force removal (i.e., disconnect the foreign key).

### v1.0.9
- Added full XML documentation for all public classes and members.
- Replaced LINQ `Where` clause foreign key lookups with dictionary-based resolution for improved performance.
- Introduced `StaticLazy<T>` as a placeholder type for referencing new (untracked) entities before attaching them to the context.
- Updated example code to demonstrate `StaticLazy` usage.
- Removed internal caching from `ILazy<T>` to avoid stale references and simplify lazy-loading logic.

### v1.0.8
- Added support for multiple `DbContext` instances using the same entity types.
- Changed internal types and members to `public` to improve extensibility.
- Moved external components and helpers into the root namespace for better discoverability.
- Introduced `ILazy<T>` to replace `Lazy<T>` with a more flexible and context-aware implementation.

### v1.0.7
- Fixed an issue with recursive read locks when accessing nested relationships.
- Removed debug console output.
- Performed general code cleanup and refactoring.

### v1.0.6
- Added support for enum properties and all primitive types.
- In-memory mode now disposes the ZIP archive immediately; `DbContext` is no longer `IDisposable`.

### v1.0.5
- Additional README improvements with more usage details.

### v1.0.4
- Fixed entity insertion bugs that caused inconsistencies during `SaveChanges`.

### v1.0.3
- Minor improvements to the README.

### v1.0.2
- README cleanup and clarifications.

### v1.0.1
- Initial README with basic usage instructions.

### v1.0.0
- Initial working prototype with basic entity tracking and ZIP-based persistence.
