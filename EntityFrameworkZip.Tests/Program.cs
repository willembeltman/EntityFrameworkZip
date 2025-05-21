
// Example test application demonstrating usage of the EntityFrameworkZip in-memory database.
// Verifies lazy loading, entity relationships, nested entity behavior, and persistence.
using EntityFrameworkZip.Tests;

// Create or load the database from a .zip file.
// All operations are performed in-memory; the zip file is only used when SaveChanges() is called.
var db = new MyDbContext(new DirectoryInfo("test")); // => Using a directory for superfast load and save

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

alice.LastModifiedDate = DateTime.Now;

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
var alice2 = company2.Employees.First(a => a.Name == "Alice");

// Verify deserialized entity properties, including enums and lazy relationships.
if (bob2 == null)
{
    throw new Exception();
}
if (bob2.LastModifiedDate != null)
{
    throw new Exception();
}
if (alice2 == null)
{
    throw new Exception();
}
if (alice2.LastModifiedDate == null)
{
    throw new Exception();
}
if (bob2.Rank != RankEnum.Second)
{
    throw new Exception("Test failed: Bob's RankEnum is not Second.");
}
if (company2.OwnerPerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Owner is not Bob after reload.");
}

db2.People.Remove(bob2, true);

if (company2.OwnerPerson.Value != null)
{
    throw new Exception("Test failed: Owner is not null after reload.");
}