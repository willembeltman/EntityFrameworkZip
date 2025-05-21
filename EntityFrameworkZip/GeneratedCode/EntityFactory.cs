namespace EntityFrameworkZip.GeneratedCode;
public partial class EntityFactory<T>
{
    private readonly Action<BinaryWriter, T, DbContext> WriteDelegate;
    private readonly Func<BinaryReader, DbContext, T> ReadDelegate;
    private readonly Action<T, DbContext> ExtendDelegate;
    private readonly Func<T, DbContext, bool, bool> FindForeignKeyUsageDelegate;

    public readonly string Code;

    public EntityFactory(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityFactory";
        var readMethodName = "EntityRead";
        var writeMethodName = "EntityWrite";
        var extendEntityMethodName = "EntityExtend";
        var findForeignKeyUsageMethodName = "EntityFindForeignKeyUsage";

        Code = $@"
            using System;
            using System.IO;
            using System.Linq;

            public static class {className}
            {{
                {GenerateSerializerCode(type, readMethodName, writeMethodName, dbContext)}
                {GenerateNavigationCode(type, extendEntityMethodName, dbContext)}
                {GenerateForeignKeyUsageCode(type, findForeignKeyUsageMethodName, dbContext)}
            }}";

        var asm = CodeCompiler.Compile(Code);
        var serializerType = asm.GetType(className)!;

        var readMethod = serializerType.GetMethod(readMethodName)!;
        var writeMethod = serializerType.GetMethod(writeMethodName)!;
        var extendEntityMethod = serializerType.GetMethod(extendEntityMethodName)!;
        var findForeignKeyUsageMethod = serializerType.GetMethod(findForeignKeyUsageMethodName)!;

        ReadDelegate = (Func<BinaryReader, DbContext, T>)Delegate.CreateDelegate(
            typeof(Func<BinaryReader, DbContext, T>), readMethod)!;

        WriteDelegate = (Action<BinaryWriter, T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<BinaryWriter, T, DbContext>), writeMethod)!;

        ExtendDelegate = (Action<T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<T, DbContext>), extendEntityMethod)!;

        FindForeignKeyUsageDelegate = (Func<T, DbContext, bool, bool>)Delegate.CreateDelegate(
            typeof(Func<T, DbContext, bool, bool>), findForeignKeyUsageMethod)!;
    }

    /// <summary>
    /// Serializes the entity and writes it to the BinaryWriter (bw)
    /// </summary>
    /// <param name="bw">The BinaryWriter bw where the entity should be written to.</param>
    /// <param name="entity">The entity to write to the BinaryWriter bw.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    public void Write(BinaryWriter bw, T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        WriteDelegate(bw, entity, dbContext);
    }
    /// <summary>
    /// Instanciates and reads the next entity from the BinaryReader br.
    /// </summary>
    /// <param name="br">The BinaryReader br where the next entity should read from.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    /// <returns>The entity read from the BinaryReader</returns>
    public T Read(BinaryReader br, DbContext dbContext)
    {
        return ReadDelegate(br, dbContext);
    }
    /// <summary>
    /// Setups all navigation properties of the entity so they can be used.
    /// </summary>
    /// <param name="entity">The entity of which the navigation properties should be set.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    public void SetNavigationProperties(T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        ExtendDelegate(entity, dbContext);
    }
    /// <summary>
    /// Finds any foreign key in the database point towards the supplied entity
    /// </summary>
    /// <param name="entity">The entity which we have to search references for.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    /// <param name="removeIfFound">Optional parameter to override set all foreign keys towards this entity to 0 or null.</param>
    /// <returns>There were any references found (and deleted/set to 0 or null)</returns>
    public bool FindForeignKeyUsage(T entity, DbContext dbContext, bool removeIfFound = false)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        return FindForeignKeyUsageDelegate(entity, dbContext, removeIfFound);
    }
}