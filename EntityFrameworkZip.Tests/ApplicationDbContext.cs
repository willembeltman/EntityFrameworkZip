using EntityFrameworkZip.Tests.Entities;
using EntityFrameworkZip;

namespace EntityFrameworkZip.Tests;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(string fullName) : base(fullName) { }
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Person> People { get; set; }
}
