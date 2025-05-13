using EntityFrameworkZip.Tests;
using EntityFrameworkZip.Tests.Entities;

ApplicationDbContext db = new ApplicationDbContext("TestDatabase");

var company = new Company() { Name = "Test Company" };
company.Employees.Add(new Person() { Name = "John Doe", Age = 30 });
company.Employees.Add(new Person() { Name = "Jane Doe", Age = 25 });
db.Companies.Add(company);
if (db.People.Count < 2)
    throw new Exception("Test failed: Company count is less than 2.");
if (!company.Employees2.Any())
    throw new Exception("Test failed: Company count is less than 2.");
db.SaveChanges();