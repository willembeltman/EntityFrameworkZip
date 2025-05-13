using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using VideoEditorD3D.Entities.ZipDatabase.Attributes;

namespace VideoEditorD3D.Entities.ZipDatabase;

/// <summary>
/// Deze class is gemaakt door ChatGPT op basis van mijn eigen implementatie die eronder staat, 
/// hij gebruikt nu het Lazy object om de code te compileren en de delegates te maken. Niet zoals 
/// ik het op zou lossen maar kennelijk is dit sneller. In grote lijnen doet deze class het volgende:
/// 
/// 1. Met behulp van reflection wordt er op basis van het type T source code genereneerd voor een 
///    serializer class die samenwerkt met een BinaryReader en BinaryWriter om alle properties van 
///    het type T te serialiseren en te deserialiseren. Dit is daadwerkelijk gewoon C# source code 
///    die on-the-fly gegenereerd wordt.
/// 2. De source code wordt gecompileerd in een assembly met behulp van Roslyn, de C# compiler.
/// 3. De assembly wordt geladen in het geheugen en de gegenereerde serializer class wordt opgehaald.
/// 4. De Write en Read methoden van de serializer class worden omgezet naar delegates die via de 
///    CachedSerializer property worden opgeslagen voor later gebruikt.
///    
/// Dit alles zorgt ervoor dat je zo snel mogelijk kan serialiseren en deserialiseren zonder dat je 
/// alle interfaces steeds moet schrijven.
/// </summary>
/// <typeparam name="T">Het type van de te serialiseren object</typeparam>
public class BinarySerializer3<T>
{
    private static readonly Lazy<(Action<BinaryWriter, T, DbContext> Writer, Func<BinaryReader, DbContext, T> Reader)> CachedSerializer =
        new(() => CreateDelegates());

    public BinarySerializer3(DbContext dbContext)
    {
    }

    private static (Action<BinaryWriter, T, DbContext>, Func<BinaryReader, DbContext, T>) CreateDelegates()
    {
        var type = typeof(T);
        var serializerClassName = $"{type.Name}BinarySerializer";
        var code = GenerateSerializerCode(type, serializerClassName);
        var asm = Compile(code);
        var serializerType = asm.GetType(serializerClassName)!;

        var writeMethod = serializerType.GetMethod("Write")!;
        var readMethod = serializerType.GetMethod("Read")!;

        var writer = (Action<BinaryWriter, T, DbContext>)Delegate.CreateDelegate(typeof(Action<BinaryWriter, T, DbContext>), writeMethod)!;
        var reader = (Func<BinaryReader, DbContext, T>)Delegate.CreateDelegate(typeof(Func<BinaryReader, DbContext, T>), readMethod)!;

        return (writer, reader);
    }

    private static string GenerateSerializerCode(Type type, string serializerName)
    {
        var itemClassName = type.Name;
        var fullClassName = type.FullName;

        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .ToArray();

        var writeCode = string.Join("\n", props.Select(p =>
            $"writer.Write(value.{p.Name});"));

        var readAssignments = string.Join("\n", props.Select(p =>
            $"{p.PropertyType.FullName} {p.Name.ToLower()} = reader.Read{GetBinaryReadMethod(p.PropertyType)}();"));

        var setProps = string.Join("\n", props.Select(p =>
            $"{p.Name} = {p.Name.ToLower()},"));

        return $@"
                using System;
                using System.IO;

                public static class {serializerName}
                {{
                    public static void Write(BinaryWriter writer, {fullClassName} value)
                    {{
                        {writeCode}
                    }}

                    public static {fullClassName} Read(BinaryReader reader)
                    {{
                        {readAssignments}
                        return new {fullClassName}
                        {{
                            {setProps}
                        }};
                    }}
                }}
            ";
    }

    private static string GetBinaryReadMethod(Type type)
    {
        if (type == typeof(int)) return "Int32";
        if (type == typeof(long)) return "Int64";
        if (type == typeof(string)) return "String";
        if (type == typeof(bool)) return "Boolean";
        if (type == typeof(float)) return "Single";
        if (type == typeof(double)) return "Double";
        throw new NotSupportedException($"Unsupported type: {type.Name}");
    }

    private static Assembly Compile(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedSerializers_" + Guid.NewGuid(),
            syntaxTrees: new[] { syntaxTree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new Exception($"Compile error:\n{errors}");
        }

        ms.Position = 0;
        return Assembly.Load(ms.ToArray());
    }

    public void Write(BinaryWriter bw, T item, DbContext dbContext) 
        => CachedSerializer.Value.Writer(bw, item, dbContext);
    public T Read(BinaryReader br, DbContext dbContext) 
        => CachedSerializer.Value.Reader(br, dbContext);
}
