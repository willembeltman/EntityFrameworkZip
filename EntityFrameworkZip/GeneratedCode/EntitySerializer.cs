using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.Reflection;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Helpers;

namespace EntityFrameworkZip.GeneratedCode;

public class EntitySerializer<T>
{
    private Action<BinaryWriter, T> WriteDelegate;
    private Func<BinaryReader, T> ReadDelegate;
    public readonly string Code;

    internal EntitySerializer()
    {
        var type = typeof(T);
        var className = $"{type.Name}EntitySerializer";
        var readMethodName = "EntitySerializerRead";
        var writeMethodName = "EntitySerializerWrite";
        Code = GenerateSerializerCode(type, className, readMethodName, writeMethodName);
        Debug.WriteLine($"Generated {className}:\r\n{Code}");
        var asm = Compile(Code);
        var serializerType = asm.GetType(className)!;
        var readMethod = serializerType.GetMethod(readMethodName)!;
        var writeMethod = serializerType.GetMethod(writeMethodName)!;

        ReadDelegate = (Func<BinaryReader, T>)Delegate.CreateDelegate(
            typeof(Func<BinaryReader, T>), readMethod)!;

        WriteDelegate = (Action<BinaryWriter, T>)Delegate.CreateDelegate(
            typeof(Action<BinaryWriter, T>), writeMethod)!;
    }

    private string GenerateSerializerCode(Type type, string serializerName, string readMethodName, string writeMethodName)
    {
        var className = type.Name;
        var fullClassName = type.FullName;

        var writeCode = string.Empty;
        var readCode = string.Empty;
        var newCode = string.Empty;

        var binarySerializerType = typeof(EntitySerializer<>);
        var binarySerializerTypeFullName = binarySerializerType.FullName!.Split('`').First();

        var entitySerializerCollectionType = typeof(EntitySerializerCollection);
        var entitySerializerCollectionTypeFullName = entitySerializerCollectionType.FullName;
        var entitySerializerCollectionTypeMethod = entitySerializerCollectionType.GetMethods().First().Name;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;

            var propertyName = prop.Name;

            if (ReflectionHelper.HasNotMappedAttribute(prop)) continue;
            if (ReflectionHelper.IsExtendedProperty(prop)) continue;
            if (ReflectionHelper.IsExtendedList(prop)) continue;

            // Wat kan er allemaal?
            //
            // Primitive types
            // DateTime
            // ExtendedProperties en ExtendedLists, mits alle keys gevonden kunnen worden en de dbset's bestaan
            // Non-generic Structs en classes (die zelfde types ondersteunen)
            // Enums (checken nog)


            //writeCode += $@"System.Windows.Forms.MessageBox.Show(""{propertyName}"");";
            //readCode += $@"System.Windows.Forms.MessageBox.Show(""{propertyName}"");";

            if (ReflectionHelper.IsPrimitiveType(prop.PropertyType))
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

                if (ReflectionHelper.IsNulleble(prop)) // || prop.PropertyType.IsValueType && 
                {
                    writeCode += @$"

                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>();
                            {propertyName}Serializer.Write(writer, value.{propertyName});
                        }}";

                    readCode += @$"

                        {prop.PropertyType} {propertyName} = null;
                        if (!reader.ReadBoolean())
                        {{
                            var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>();
                            {propertyName} = {propertyName}Serializer.Read(reader);
                        }}";

                }
                else
                {
                    writeCode += @$"

                        var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>();
                        {propertyName}Serializer.Write(writer, value.{propertyName});";

                    readCode += @$"

                        var {propertyName}Serializer = {entitySerializerCollectionTypeFullName}.{entitySerializerCollectionTypeMethod}<{prop.PropertyType.FullName}>();
                        var {propertyName} = {propertyName}Serializer.Read(reader);";

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
                public static void {writeMethodName}(BinaryWriter writer, {fullClassName} value)
                {{{writeCode}
                }}

                public static {fullClassName} {readMethodName}(BinaryReader reader)
                {{{readCode}

                    var item = new {fullClassName}
                    {{{newCode}
                    }};

                    return item;
                }}
            }}";
    }

    private static string GetBinaryWriteMethod(Type type, string propertyName)
    {
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.ToBinary());";
        return $"writer.Write(value.{propertyName})";
    }
    private static string GetBinaryReadMethod(Type type)
    {
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

    private Assembly Compile(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "GeneratedEntitySerializers",
            new[] { syntaxTree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new Exception($"Compile error:\n{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }

    public void Write(BinaryWriter bw, T item)
    {
        WriteDelegate(bw, item);
    }
    public T Read(BinaryReader bw)
    {
        return ReadDelegate(bw);
    }
}
