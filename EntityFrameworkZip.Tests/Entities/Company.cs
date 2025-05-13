using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip.Tests.Entities;

public class Company : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();
    public virtual IEnumerable<Person> Employees2 { get; set; } = new List<Person>();
}
