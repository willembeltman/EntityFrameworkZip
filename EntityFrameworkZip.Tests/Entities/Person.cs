using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip.Tests.Entities;

#nullable disable
public class Person : IEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; }

    public virtual Lazy<Company> Company { get; set; } = new Lazy<Company>(() => null);
}
