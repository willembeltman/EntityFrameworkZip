using EntityFrameworkZip;
using EntityFrameworkZip.Interfaces;
using EntityFrameworkZip.Tests;
using EntityFrameworkZip.Tests.Entities;

//ApplicationDbContext db = new ApplicationDbContext("TestDatabase");

//var company = new Company() { Name = "Test Company" };
//company.Employees.Add(new Person() { Name = "John Doe", Age = 30 });
//company.Employees.Add(new Person() { Name = "Jane Doe", Age = 25 });
//db.Companies.Add(company);
//if (db.People.Count < 2)
//    throw new Exception("Test failed: Company count is less than 2.");
//if (!company.Employees2.Any())
//    throw new Exception("Test failed: Company count is less than 2.");
//db.SaveChanges();


#nullable disable

// Create or load a database from a zip file.
var db = new ApplicationDbContext("test.zip");

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
