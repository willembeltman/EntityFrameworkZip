// Example test application demonstrating usage of the EntityFrameworkZip in-memory database.
using EntityFrameworkZip.Tests;

// Create or load the database from a .zip file.
// All operations are performed in-memory; the zip file is only used when SaveChanges() is called.
var db = new MyDbContext("test.zip");

// Create Person entities.
var alice = new Person { Name = "Alice" };
var bob = new Person { Name = "Bob" };
bob.TestEnum = TestEnum.Second;

// Add persons to the context.
db.People.Add(alice);
db.People.Add(bob);

// Create a Company and assign employees *before* adding the company to the context.
// Because the Employees list is initialized in the Company class, we can safely add employees here.
var testCompany = new Company { Name = "Test Company" };
testCompany.Employees.Add(alice);
testCompany.Employees.Add(bob);

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

// Set the owner relationship explicitly.
// Note: OwnerId must be set manually; the OwnerPerson lazy reference will resolve after adding to the context.
testCompany.OwnerId = bob.Id;

// Verify that lazy loading of the OwnerPerson property works correctly.
if (testCompany.OwnerPerson.Value.Name != "Bob")
{
    throw new Exception("Test failed: Owner is not Alice.");
}

// Query example: select all people whose name starts with "A".
var all = db.People.Where(p => p.CompanyId == testCompany.Id && p.Name.StartsWith('A')).ToList();
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
var company2 = db2.Companies.Last();
var bob2 = company2.Employees.First(a => a.Name == "Bob");
if (bob2.TestEnum != TestEnum.Second)
{
    throw new Exception("Test failed: Bob's TestEnum is not Second.");
}