using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Helpers;

namespace EntityFrameworkZip.GeneratedCode;

/// <summary>
/// Dynamisch gegenereerde serializer voor een entity van type <typeparamref name="T"/>.
/// Maakt het mogelijk om entities snel te serialiseren en deserialiseren naar binaire data,
/// met gebruik van een DbContext voor eventuele gerelateerde entiteiten.
/// </summary>
/// <typeparam name="T">Het type entity dat geserialiseerd wordt.</typeparam>
public class EntitySerializer<T> : CodeCompiler
{
    private readonly Action<BinaryWriter, T, DbContext> WriteDelegate;
    private readonly Func<BinaryReader, DbContext, T> ReadDelegate;

    /// <summary>
    /// De gegenereerde broncode van de serializer klasse als string.
    /// </summary>
    public readonly string Code;

    /// <summary>
    /// Serialiseert een instantie van <typeparamref name="T"/> naar een binaire stream.
    /// </summary>
    /// <param name="bw">De BinaryWriter om de data naar te schrijven.</param>
    /// <param name="item">De entity instantie die geserialiseerd wordt.</param>
    /// <param name="dbContext">De DbContext die kan worden gebruikt tijdens het schrijven.</param>
    public void Write(BinaryWriter bw, T item, DbContext dbContext)
    {
        WriteDelegate(bw, item, dbContext);
    }
    /// <summary>
    /// Deserialiseert een instantie van <typeparamref name="T"/> vanuit een binaire stream.
    /// </summary>
    /// <param name="br">De BinaryReader om de data uit te lezen.</param>
    /// <param name="dbContext">De DbContext die kan worden gebruikt tijdens het lezen.</param>
    /// <returns>Een nieuw object van type <typeparamref name="T"/> die is opgebouwd uit de binaire data.</returns>
    public T Read(BinaryReader bw, DbContext dbContext)
    {
        return ReadDelegate(bw, dbContext);
    }

    /// <summary>
    /// Initialiseert een nieuwe instantie van de <see cref="EntitySerializer{T}"/> klasse,
    /// genereert en compileert serializer code voor het type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="dbContext">De DbContext die wordt gebruikt bij serialisatie en deserialisatie.</param>
    public EntitySerializer(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}EntitySerializer";
        var readMethodName = "EntitySerializerRead";
        var writeMethodName = "EntitySerializerWrite";

        Code = GenerateSerializerCode(type, className, readMethodName, writeMethodName, dbContext);

        var asm = Compile(Code);
        var serializerType = asm.GetType(className)!;
        var readMethod = serializerType.GetMethod(readMethodName)!;
        var writeMethod = serializerType.GetMethod(writeMethodName)!;

        ReadDelegate = (Func<BinaryReader, DbContext, T>)Delegate.CreateDelegate(
            typeof(Func<BinaryReader, DbContext, T>), readMethod)!;

        WriteDelegate = (Action<BinaryWriter, T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<BinaryWriter, T, DbContext>), writeMethod)!;
    }

    private static string GenerateSerializerCode(Type type, string serializerName, string readMethodName, string writeMethodName, DbContext dbContext)
    {
        var fullClassName = type.FullName;

        var writeCode = string.Empty;
        var readCode = string.Empty;
        var newCode = string.Empty;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        //var binarySerializerType = typeof(EntitySerializer<>);
        //var binarySerializerTypeFullName = binarySerializerType.FullName!.Split('`').First();

        var entitySerializerCollectionType = typeof(EntitySerializerCollection);
        var entitySerializerCollectionTypeFullName = entitySerializerCollectionType.FullName;
        var entitySerializerCollectionTypeMethod = entitySerializerCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;

            var propertyName = prop.Name;

            if (ReflectionHelper.HasNotMappedAttribute(prop)) continue;
            if (ReflectionHelper.IsOldExtendedForeignEntityProperty(prop))
                throw new Exception(
                    "As of version 1.0.8, we’ve moved from Lazy<T> to ILazy<T> to fix a subtle bug with foreign keys not updating.\n" +
                    "Sorry for the breaking change — you'll need to update your entity properties.\n" +
                    "Read more at: https://github.com/willembeltman/EntityFrameworkZip"
                ); 

            if (ReflectionHelper.IsExtendedForeignEntityProperty(prop)) continue;
            if (ReflectionHelper.IsExtendedForeignListProperty(prop)) continue;

            if (ReflectionHelper.IsPrimitiveTypeOrEnum(prop.PropertyType))
            {
                var readMethod = GetBinaryReadMethod(prop.PropertyType);
                var writeMethod = GetBinaryWriteMethod(prop.PropertyType, propertyName);

                if (ReflectionHelper.IsNulleble(prop))
                {
                    writeCode += @$"
                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            {writeMethod};
                        }}";

                    readCode += @$"
                        {prop.PropertyType.FullName} {propertyName} = null;
                        if (!reader.ReadBoolean())
                        {{
                            {propertyName} = {readMethod};
                        }}";

                }
                else
                {
                    writeCode += @$"
                        {writeMethod};";

                    readCode += @$"
                        var {propertyName} = {readMethod};";
                }
            }
            else
            {
                if (ReflectionHelper.IsGenericType(prop.PropertyType))
                    throw new Exception(
                        $"ZipDatabase Exception: Property '{propertyName}' of entity type '{type.FullName}' is not marked as [NotMapped] and is a " +
                        $"{prop.PropertyType} type, which is not a valid type to serialize. The ZipDatabase engine " +
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

                if (!ReflectionHelper.IsValidChildEntity(prop.PropertyType))
                    throw new Exception(
                        $"ZipDatabase Exception: Child entity type '{prop.PropertyType}' of property '{propertyName}' of (child) entity type '{type.FullName}', " +
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
                            var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                            {propertyName}Serializer.Write(writer, value.{propertyName}, db);
                        }}";

                    readCode += @$"

                        {prop.PropertyType} {propertyName} = null;
                        if (!reader.ReadBoolean())
                        {{
                            var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                            {propertyName} = {propertyName}Serializer.Read(reader, db);
                        }}";

                }
                else
                {
                    writeCode += @$"

                        var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                        {propertyName}Serializer.Write(writer, value.{propertyName}, db);";

                    readCode += @$"

                        var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                        var {propertyName} = {propertyName}Serializer.Read(reader, db);";

                }
            }

            newCode += @$"
                {propertyName} = {propertyName},";
        }

        return $@"
            using System;
            using System.IO;
            using System.Linq;

            public static class {serializerName}
            {{
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
            }}";
    }

    private static string GetBinaryWriteMethod(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName})";
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.ToBinary());";
        return $"writer.Write(value.{propertyName})";
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
}