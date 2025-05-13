# EntityFrameworkZip

A lightweight, in-memory, EF-style database that can be saved to and loaded from a `.zip` file.

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

## 🚀 Getting Started

```csharp

#nullable disable

// A simple Person entity implementing IEntity.
// Includes a reference to a Company via a Lazy<Company> property.
public class Person : IEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; }

    // Lazily loaded reference to the related Company.
    public virtual Lazy<Company> Company { get; set; } = new Lazy<Company>(() => null);
}

#nullable disable

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
    public virtual Lazy<Person> Owner { get; set; } = new Lazy<Person>(() => null);
}

#nullable disable

// The database context. Inherits from the Zip-based EF-like memory database.
public class MyDbContext : DbContext
{
    public MyDbContext(string fullName) : base(fullName) { }

    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Person> People { get; set; }
}

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

## 🧠 When to Use

✅ Unit testing without mocking databases

✅ Desktop apps with lightweight persistence needs

✅ Quick prototypes and demos

✅ Tools that bundle config/data/state in a single zip file

✅ Save game systems or file-based data snapshots

---

## ❗ Notes

This is an in-memory database — data is only saved if you explicitly call SaveChanges()

All entities must implement the IEntity interface (with at least an Id property)

Foreign key relationships are automatically managed 

---

## 📄 License

MIT — free to use and modify.

---

## 💬 Feedback or Contributions?

Found a bug? Want to suggest a feature? Feel free to open an issue or contribute via pull request!