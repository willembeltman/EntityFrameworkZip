using EntityFrameworkZip.Collections;
using EntityFrameworkZip.ExtendedEntity;
using EntityFrameworkZip.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkZip.GeneratedCode;
public class EntityHelper<T> : CodeCompiler
{
    private readonly Action<BinaryWriter, T, DbContext> WriteDelegate;
    private readonly Func<BinaryReader, DbContext, T> ReadDelegate;
    private readonly Action<T, DbContext> ExtendEntityDelegate;
    private readonly Func<T, DbContext, bool, bool> FindForeignKeyUsageDelegate;

    public readonly string Code;

    public void Write(BinaryWriter bw, T item, DbContext dbContext)
    {
        WriteDelegate(bw, item, dbContext);
    }
    public T Read(BinaryReader bw, DbContext dbContext)
    {
        return ReadDelegate(bw, dbContext);
    }
    public void ExtendEntity(T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        ExtendEntityDelegate(entity, dbContext);
    }
    public bool FindForeignKeyUsage(T entity, DbContext dbContext, bool removeIfFound = false)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        return FindForeignKeyUsageDelegate(entity, dbContext, removeIfFound);
    }

    public EntityHelper(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityHelper";
        var readMethodName = "EntityHelperRead";
        var writeMethodName = "EntityHelperWrite";
        var extendEntityMethodName = "ExtendEntity";
        var findForeignKeyUsageMethodName = "FindForeignKeyUsage";

        Code = $@"
            using System;
            using System.IO;
            using System.Linq;

            public static class {className}
            {{
                {GenerateSerializerCode(type, readMethodName, writeMethodName, dbContext)}
                {GenerateExtenderCode(type, extendEntityMethodName, dbContext)}
                {GenerateFinderCode(type, findForeignKeyUsageMethodName, dbContext)}
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

        ExtendEntityDelegate = (Action<T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<T, DbContext>), extendEntityMethod)!;

        FindForeignKeyUsageDelegate = (Func<T, DbContext, bool, bool>)Delegate.CreateDelegate(
            typeof(Func<T, DbContext, bool, bool>), findForeignKeyUsageMethod)!;
    }

    private static string GenerateSerializerCode(Type type, string readMethodName, string writeMethodName, DbContext dbContext)
    {
        var fullClassName = type.FullName;

        var writeCode = string.Empty;
        var readCode = string.Empty;
        var newCode = string.Empty;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var entityHelperCollectionType = typeof(EntityHelperCollection);
        var entityHelperCollectionTypeFullName = entityHelperCollectionType.FullName;
        var entityHelperCollectionTypeMethod = entityHelperCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            if (ReflectionHelper.HasNotMappedAttribute(prop)) continue;

            if (ReflectionHelper.IsOldExtendedForeignEntityProperty(prop))
                throw new Exception(
                    "As of version 1.0.8, we’ve moved from Lazy<T> to ILazy<T> to fix a subtle bug with foreign keys not updating.\n" +
                    "Sorry for the breaking change — you'll need to update your entity properties.\n" +
                    "Read more at: https://github.com/willembeltman/EntityFrameworkZip"
                );

            if (ReflectionHelper.IsExtendedForeignProperty(prop)) continue;

            var propertyName = prop.Name;
            var propertyType = ReflectionHelper.GetUnderlyingType(prop.PropertyType);

            if (ReflectionHelper.IsPrimitiveTypeOrEnum(propertyType))
            {
                if (ReflectionHelper.IsNulleble(prop))
                {
                    var readMethod = GetBinaryReadMethod(propertyType);
                    var writeMethod = GetBinaryWriteMethodNull(propertyType, propertyName);

                    writeCode += @$"
                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            {writeMethod};
                        }}";

                    readCode += @$"
                        {propertyType.FullName}? {propertyName} = null;
                        if (!reader.ReadBoolean())
                        {{
                            {propertyName} = {readMethod};
                        }}";

                }
                else
                {
                    var readMethod = GetBinaryReadMethod(propertyType);
                    var writeMethod = GetBinaryWriteMethodNotNull(propertyType, propertyName);

                    writeCode += @$"
                        {writeMethod};";

                    readCode += @$"
                        var {propertyName} = {readMethod};";
                }
            }
            else
            {
                if (ReflectionHelper.IsGenericType(propertyType))
                    throw new Exception(
                        $"ZipDatabase Exception: Property '{propertyName}' of entity type '{type.FullName}' is not marked as [NotMapped] and is a " +
                        $"{propertyType} type, which is not a valid type to serialize. The ZipDatabase engine " +
                        $"does not support generic types like Lists and Array, it only support:\r\n" +
                        $"\r\n" +
                        $"- Primative types.\r\n" +
                        $"- DateTime's.\r\n" +
                        $"- ExtendedProperties with 'virtual Lazy<>' (with optional ForeignKeyAttribute).\r\n" +
                        $"- ExtendedLists with 'virtual ICollection<>' or 'virtual IEnumerable<>' (with optional ForeignKeyAttribute).\r\n" +
                        $"- And non-generic struct's and classes(which in turn support the same kind of properties). \r\n" +
                        $"\r\n" +
                        $"You can ofcourse use generic types like Lists or Arrays but only marked as [NotMapped] " +
                        $"to signal those properties are not serialized when the dbcontext is saved. Please mark " +
                        $"those properties as [NotMapped] if this is intended.");

                if (!ReflectionHelper.IsValidChildEntity(propertyType))
                    throw new Exception(
                        $"ZipDatabase Exception: Child entity type '{propertyType}' of property '{propertyName}' of (child) entity type '{type.FullName}', " +
                        $"contains non-primitive types or generic classes that aren't marked as [NotMapped]. The ZipDatabase engine " +
                        $"does not support serialisation of non-primitive types or generic classes like Lists or Arrays. The ZipDatabase engine " +
                        $"only supports child entity types containing:\r\n" +
                        $"\r\n" +
                        $"- Primative types.\r\n" +
                        $"- DateTime's.\r\n" +
                        $"- ExtendedProperties with 'virtual Lazy<>' (with optional ForeignKeyAttribute).\r\n" +
                        $"- ExtendedLists with 'virtual ICollection<>' or 'virtual IEnumerable<>' (with optional ForeignKeyAttribute).\r\n" +
                        $"- And non-generic struct's and classes(which in turn support the same). \r\n" +
                        $"\r\n" +
                        $"You can ofcourse use generic types like Lists or Arrays but only marked as [NotMapped] " +
                        $"to signal those properties are not serialized when the dbcontext is saved. Please mark " +
                        $"those properties as [NotMapped] if this is intended.");

                if (ReflectionHelper.IsNulleble(prop))
                {
                    writeCode += @$"

                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            var {propertyName}Serializer = {entityHelperCollectionTypeFullName}.{entityHelperCollectionTypeMethod}<{propertyType.FullName}>(db);
                            {propertyName}Serializer.Write(writer, value.{propertyName}, db);
                        }}";

                    readCode += @$"

                        {propertyType} {propertyName} = null;
                        if (!reader.ReadBoolean())
                        {{
                            var {propertyName}Serializer = {entityHelperCollectionTypeFullName}.{entityHelperCollectionTypeMethod}<{propertyType.FullName}>(db);
                            {propertyName} = {propertyName}Serializer.Read(reader, db);
                        }}";

                }
                else
                {
                    writeCode += @$"

                        var {propertyName}Serializer = {entityHelperCollectionTypeFullName}.{entityHelperCollectionTypeMethod}<{propertyType.FullName}>(db);
                        {propertyName}Serializer.Write(writer, value.{propertyName}, db);";

                    readCode += @$"

                        var {propertyName}Serializer = {entityHelperCollectionTypeFullName}.{entityHelperCollectionTypeMethod}<{propertyType.FullName}>(db);
                        var {propertyName} = {propertyName}Serializer.Read(reader, db);";

                }
            }

            newCode += @$"
                {propertyName} = {propertyName},";
        }

        return $@"
                public static void {writeMethodName}(BinaryWriter writer, {fullClassName} value, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    {writeCode}
                }}

                public static {fullClassName} {readMethodName}(BinaryReader reader, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextTypeFullName}"");
                    {readCode}

                    var item = new {fullClassName}
                    {{{newCode}
                    }};

                    return item;
                }}
                ";
    }

    private static string GetBinaryWriteMethodNotNull(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName})";
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.ToBinary());";
        return $"writer.Write(value.{propertyName})";
    }
    private static string GetBinaryWriteMethodNull(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName}.Value)";
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.Value.ToBinary());";
        if (type == typeof(string)) return $"writer.Write(value.{propertyName})";
        return $"writer.Write(value.{propertyName}.Value)";
    }
    private static string GetBinaryReadMethod(Type type)
    {
        if (type.IsEnum) return $"({type.FullName})reader.ReadInt32()";
        if (type == typeof(bool)) return "reader.ReadBoolean()";
        if (type == typeof(byte)) return "reader.ReadByte()";
        if (type == typeof(sbyte)) return "reader.ReadSByte()";
        if (type == typeof(char)) return "reader.ReadChar()";
        if (type == typeof(decimal)) return "reader.ReadDecimal()";
        if (type == typeof(double)) return "reader.ReadDouble()";
        if (type == typeof(float)) return "reader.ReadSingle()";
        if (type == typeof(short)) return "reader.ReadInt16()";
        if (type == typeof(ushort)) return "reader.ReadUInt16()";
        if (type == typeof(int)) return "reader.ReadInt32()";
        if (type == typeof(uint)) return "reader.ReadUInt32()";
        if (type == typeof(long)) return "reader.ReadInt64()";
        if (type == typeof(ulong)) return "reader.ReadUInt64()";
        if (type == typeof(string)) return "reader.ReadString()";
        if (type == typeof(DateTime)) return "DateTime.FromBinary(reader.ReadInt64())";
        throw new Exception($"Type {type.Name} not supported while its added to the ReflectionHelper.IsPrimitiveType list.");
    }

    private static string GenerateExtenderCode(Type type, string methodName, DbContext dbContext)
    {
        var className = type.Name;
        var fullClassName = type.FullName;

        var lazyCode = string.Empty;

        var foreignEntityCollectionNotNullType = typeof(LazyForeignEntityCollectionNotNull<,>);
        var foreignEntityCollectionNotNullFullName = foreignEntityCollectionNotNullType.FullName!.Split('`').First();

        var foreignEntityCollectionNullType = typeof(LazyForeignEntityCollectionNull<,>);
        var foreignEntityCollectionNullFullName = foreignEntityCollectionNullType.FullName!.Split('`').First();

        var foreignEntityLazyNotNullType = typeof(LazyForeignEntityNotNull<,>);
        var foreignEntityLazyNotNullFullName = foreignEntityLazyNotNullType.FullName!.Split('`').First();

        var foreignEntityLazyNullType = typeof(LazyForeignEntityNull<,>);
        var foreignEntityLazyNullFullName = foreignEntityLazyNullType.FullName!.Split('`').First();

        var entityHelperType = typeof(EntityHelper<>);
        var entityHelperFullName = entityHelperType.FullName!.Split('`').First();

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var entityHelperCollectionType = typeof(EntityHelperCollection);
        var entityHelperCollectionTypeFullName = entityHelperCollectionType.FullName;
        var entityHelperCollectionTypeMethod = entityHelperCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            var propertyName = prop.Name;
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            if (ReflectionHelper.HasExtendedForeignProperties(prop.PropertyType))
            {
                lazyCode += $@"
                    if (item.{propertyName} != null)
                    {{
                        var {propertyName}Extender = {entityHelperCollectionTypeFullName}.{entityHelperCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                        {propertyName}Extender.ExtendEntity(item.{propertyName}, db);
                    }}";
                continue;
            }
            if (!ReflectionHelper.IsExtendedForeignProperty(prop)) continue;

            if (ReflectionHelper.IsExtendedForeignListProperty(prop))
            {
                var foreignType = ReflectionHelper.GetIEnumerableType(prop);
                var foreignKeyName = $"{className}Id";
                if (ReflectionHelper.HasForeignKeyAttribute(prop))
                {
                    foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(prop);
                }

                var foreignProperty = foreignType.GetProperties()
                    .FirstOrDefault(a => a.Name == foreignKeyName)
                    ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {foreignType.FullName}.");

                var foreignPropertyOnApplicationDbContext = applicationDbContextType.GetProperties()
                    .Where(a => ReflectionHelper.IsDbSet(a))
                    .FirstOrDefault(a => ReflectionHelper.GetDbSetType(a) == foreignType)
                    ?? throw new Exception($"ZipDatabase Exception: DbSet<{foreignType.Name}> not found on {applicationDbContextType.Name}.");

                if (!ReflectionHelper.HasIEntityInterface(type))
                    throw new Exception(
                        $"ZipDatabase Exception: Type '{type.FullName}' does not implement IEntity interface, though is used to filter in the " +
                        $"{foreignType.Name} entities with '{foreignKeyName}'. Type {type.Name} needs a primary key " +
                        $"('public long Id {{ get; set; }}' property) to filter in Entities (you can copy it from the " +
                        $"parent entity '{foreignType}').");

                var foreignPropertyOnApplicationDbContextName = foreignPropertyOnApplicationDbContext.Name;


                if (ReflectionHelper.IsNulleble(foreignProperty))
                {
                    lazyCode += $@"
                    if (item.{propertyName} != null &&
                        item.{propertyName}.GetType() != typeof({foreignEntityCollectionNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        foreach(var subitem in item.{propertyName})
                        {{
                            if (subitem.{foreignKeyName} != item.Id)
                                subitem.{foreignKeyName} = item.Id;
                            db.{foreignPropertyOnApplicationDbContextName}.Attach(subitem);
                        }}
                    }}
                    if (item.{propertyName} == null ||
                        item.{propertyName}.GetType() != typeof({foreignEntityCollectionNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        item.{propertyName} = new {foreignEntityCollectionNullFullName}<{foreignType.FullName}, {fullClassName}>(
                            db.{foreignPropertyOnApplicationDbContextName},
                            item,
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = value; }});
                    }}";
                }
                else
                {
                    lazyCode += $@"
                    if (item.{propertyName} != null &&
                        item.{propertyName}.GetType() != typeof({foreignEntityCollectionNotNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        foreach(var subitem in item.{propertyName})
                        {{
                            if (subitem.{foreignKeyName} != item.Id)
                                subitem.{foreignKeyName} = item.Id;
                            db.{foreignPropertyOnApplicationDbContextName}.Attach(subitem);
                        }}
                    }}
                    if (item.{propertyName} == null ||
                        item.{propertyName}.GetType() != typeof({foreignEntityCollectionNotNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        item.{propertyName} = new {foreignEntityCollectionNotNullFullName}<{foreignType.FullName}, {fullClassName}>(
                            db.{foreignPropertyOnApplicationDbContextName},
                            item,
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = value; }});
                    }}";
                }

            }
            else if (ReflectionHelper.IsExtendedForeignEntityProperty(prop))
            {
                var foreignType = ReflectionHelper.GetILazyType(prop);
                var foreignKeyName = $"{propertyName}Id";
                if (ReflectionHelper.HasForeignKeyAttribute(prop))
                {
                    foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(prop);
                }

                var foreignProperty = type.GetProperties()
                    .FirstOrDefault(a => a.Name == foreignKeyName)
                    ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {type.Name}.");

                var lazyPropertyOnApplicationDbContext = applicationDbContextType.GetProperties()
                    .Where(a => ReflectionHelper.IsDbSet(a))
                    .FirstOrDefault(a => ReflectionHelper.GetDbSetType(a) == foreignType);
                if (lazyPropertyOnApplicationDbContext == null) continue;

                var lazyPropertyOnApplicationDbContextName = lazyPropertyOnApplicationDbContext.Name;

                if (ReflectionHelper.IsNulleble(foreignProperty))
                {
                    lazyCode += @$"
                    if (item.{propertyName} != null && 
                        item.{propertyName}.GetType() != typeof({foreignEntityLazyNullFullName}<{foreignType.FullName}, {fullClassName}>) &&
                        item.{propertyName}.Value != null)
                    {{
                        var subitem = item.{propertyName}.Value;
                        db.{lazyPropertyOnApplicationDbContextName}.Attach(subitem);
                        if (item.{foreignKeyName} != subitem.Id)
                            item.{foreignKeyName} = subitem.Id;
                    }}

                    if (item.{propertyName} == null ||
                        item.{propertyName}.GetType() != typeof({foreignEntityLazyNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        item.{propertyName} = new {foreignEntityLazyNullFullName}<{foreignType.FullName}, {fullClassName}>(
                            db.{lazyPropertyOnApplicationDbContextName},
                            item,
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = value; }});
                    }}";
                }
                else
                {
                    lazyCode += @$"
                    if (item.{propertyName} != null && 
                        item.{propertyName}.GetType() != typeof({foreignEntityLazyNotNullFullName}<{foreignType.FullName}, {fullClassName}>) &&
                        item.{propertyName}.Value != null)
                    {{
                        var subitem = item.{propertyName}.Value;
                        db.{lazyPropertyOnApplicationDbContextName}.Attach(subitem);
                        if (item.{foreignKeyName} != subitem.Id)
                            item.{foreignKeyName} = subitem.Id;
                    }}

                    if (item.{propertyName} == null ||
                        item.{propertyName}.GetType() != typeof({foreignEntityLazyNotNullFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        item.{propertyName} = new {foreignEntityLazyNotNullFullName}<{foreignType.FullName}, {fullClassName}>(
                            db.{lazyPropertyOnApplicationDbContextName},
                            item,
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = value; }});
                    }}";
                }

            }
        }

        return $@"
                public static void {methodName}({fullClassName} item, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextTypeFullName}"");
                    {lazyCode}
                }}
                ";
    }

    private static string GenerateFinderCode(Type type, string methodName, DbContext dbContext)
    {
        var className = type.Name;
        var fullClassName = type.FullName;

        var codeRemoveIfFound = string.Empty;
        var codeExceptionIfFound = string.Empty;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;
        if (applicationDbContextTypeFullName == null)
            throw new Exception("Cannot find dbcontext type");

        var applicationDbContextProps = applicationDbContextType.GetProperties();

        foreach (var applicationDbContextProp in applicationDbContextProps)
        {
            var applicationDbContextPropType = applicationDbContextProp.PropertyType;
            if (!ReflectionHelper.IsDbSet(applicationDbContextProp)) continue;

            var entityType = ReflectionHelper.GetDbSetType(applicationDbContextPropType);
            if (entityType == null) continue;
            if (entityType == type) continue;

            var dbSetName = applicationDbContextProp.Name;
            var listIndex = 0;
            CheckEntity(type, entityType, dbSetName, applicationDbContextTypeFullName,
                ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex);
        }

        return $@"
                public static bool {methodName}({fullClassName} item, {dbContextTypeFullName} objDb, bool removeIfFound)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextTypeFullName}"");
                    var res = false;
                    if (removeIfFound)
                    {{{codeRemoveIfFound}
                    }}
                    else
                    {{{codeExceptionIfFound}
                    }}
                    return res;
                }}
                ";
    }
    private static void CheckEntity(Type type, Type entityType, string dbSetName, string applicationDbContextTypeFullName,
        ref string codeRemoveIfFound, ref string codeExceptionIfFound, ref int listIndex,
        List<Type>? doneTypes = null, string baseProperty = "", string basePropertyNull = "")
    {
        if (doneTypes == null)
        {
            doneTypes = new List<Type>();
        }
        else
        {
            if (doneTypes.Contains(entityType)) return;
        }
        doneTypes.Add(entityType);

        var entityProps = entityType.GetProperties();
        foreach (var entityProp in entityProps)
        {
            var propertyName = entityProp.Name;
            if (!ReflectionHelper.HasPublicGetter(entityProp)) continue;
            if (!ReflectionHelper.HasPublicSetter(entityProp)) continue;
            if (ReflectionHelper.HasExtendedForeignProperties(entityProp.PropertyType))
            {
                CheckEntity(type, entityProp.PropertyType, dbSetName, applicationDbContextTypeFullName,
                    ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex,
                    doneTypes, baseProperty + $".{entityProp.Name}", basePropertyNull + $".{entityProp.Name}?");
                continue;
            }
            if (!ReflectionHelper.IsExtendedForeignEntityProperty(entityProp)) continue;

            var foreignType = ReflectionHelper.GetILazyType(entityProp);
            var foreignKeyName = $"{propertyName}Id";
            if (ReflectionHelper.HasForeignKeyAttribute(entityProp))
            {
                foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(entityProp);
            }

            var foreignProperty = entityType.GetProperties()
                .FirstOrDefault(a => a.Name == foreignKeyName)
                ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {type.Name}.");

            var defaultvalue = ReflectionHelper.IsNulleble(foreignProperty) ? "null" : "0";

            var entityTypeName = entityType.Name;

            listIndex++;
            codeRemoveIfFound += @$"
                        var list{listIndex} = db.{dbSetName}.Where(a => a{basePropertyNull}.{foreignKeyName} == item.Id);
                        foreach (var item{listIndex} in list{listIndex})
                        {{
                            res = true;
                            item{listIndex}{baseProperty}.{foreignKeyName} = {defaultvalue};
                        }}";
            codeExceptionIfFound += @$"
                        if (db.{dbSetName}.Any(a => a{basePropertyNull}.{foreignKeyName} == item.Id)) 
                            throw new Exception(""Cannot delete {type.FullName}, id #"" + item.Id + "", from {type.Name}. {applicationDbContextTypeFullName}.{dbSetName}{baseProperty}.{foreignKeyName} has a reference towards it. Please remove the reference."");";

        }
    }
}
