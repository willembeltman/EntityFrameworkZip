using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip.Tests.Entities;

#nullable disable
public class Company : IEntity
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();
    public virtual Lazy<Person> Owner { get; set; } = new Lazy<Person>(() => null);
}
