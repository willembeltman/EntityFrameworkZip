namespace EntityFrameworkZip.Collections;

internal struct EntitySerializerKey
{
    internal EntitySerializerKey(Type type, Type applicationDbContextType) : this()
    {
        Type = type;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type Type { get; }
    public Type ApplicationDbContextType { get; }
}
