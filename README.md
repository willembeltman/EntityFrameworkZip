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

public class Person : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
}

public class MyDbContext : ZipDbContext
{
    public MyDbContext() : base("mydata.zip") { }
    public virtual DbSet<Person> People { get; set; }
}

var db = new MyDbContext();

// Add entities
db.People.Add(new Person { Name = "Alice" });
db.People.Add(new Person { Name = "Bob" });


// Query
var all = db.People.Where(p => p.Name.StartsWith("A")).ToList();

// Save to zip
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

Foreign key relationships can be automatically managed 

---

## 📄 License

MIT — free to use and modify.

---

## 💬 Feedback or Contributions?

Found a bug? Want to suggest a feature? Feel free to open an issue or contribute via pull request!