namespace EntityFrameworkZip.Collections;

internal struct EntityExtenderKey
{
    internal EntityExtenderKey(Type type, Type applicationDbContextType) : this()
    {
        Type = type;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type Type { get; }
    public Type ApplicationDbContextType { get; }
}
