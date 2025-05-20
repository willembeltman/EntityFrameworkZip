namespace EntityFrameworkZip.Collections;

public readonly struct ForeignKeyUsageFinderKey
{
    public ForeignKeyUsageFinderKey(Type type, Type applicationDbContextType) : this()
    {
        Type = type;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type Type { get; }
    public Type ApplicationDbContextType { get; }
}
