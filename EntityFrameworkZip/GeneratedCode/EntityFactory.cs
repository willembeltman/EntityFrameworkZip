using EntityFrameworkZip.Collections;
using EntityFrameworkZip.ExtendedEntity;
using EntityFrameworkZip.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkZip.GeneratedCode;
public partial class EntityFactory<T> : CodeCompiler
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
                {GenerateExtenderCode(type, extendEntityMethodName, dbContext)}
                {GenerateForeignKeyUsageCode(type, findForeignKeyUsageMethodName, dbContext)}
            }}";

        var asm = Compile(Code);
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

    public void Write(BinaryWriter bw, T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        WriteDelegate(bw, entity, dbContext);
    }
    public T Read(BinaryReader bw, DbContext dbContext)
    {
        return ReadDelegate(bw, dbContext);
    }
    public void Extend(T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        ExtendDelegate(entity, dbContext);
    }
    public bool FindForeignKeyUsage(T entity, DbContext dbContext, bool removeIfFound = false)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        return FindForeignKeyUsageDelegate(entity, dbContext, removeIfFound);
    }
}