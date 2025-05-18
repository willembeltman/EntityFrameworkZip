namespace EntityFrameworkZip;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute
{
    public ForeignKeyAttribute(string name) : base(name)
    {
    }
}