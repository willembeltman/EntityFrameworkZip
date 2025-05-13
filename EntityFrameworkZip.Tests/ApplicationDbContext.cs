using EntityFrameworkZip.Tests.Entities;

namespace EntityFrameworkZip.Tests;

#nullable disable
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(string fullName) : base(fullName) { }
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Person> People { get; set; }
}
