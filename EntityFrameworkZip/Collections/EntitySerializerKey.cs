namespace EntityFrameworkZip.Collections;

public struct EntitySerializerKey
{
    public EntitySerializerKey(Type type, Type applicationDbContextType) : this()
    {
        Type = type;
        ApplicationDbContextType = applicationDbContextType;
    }

    public Type Type { get; }
    public Type ApplicationDbContextType { get; }
}
