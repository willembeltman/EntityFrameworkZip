namespace EntityFrameworkZip.Collections;

public struct EntityExtenderKey
{
    public EntityExtenderKey(Type type, Type applicationDbContextType) : this()
    {
        Type = type;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type Type { get; }
    public Type ApplicationDbContextType { get; }
}
