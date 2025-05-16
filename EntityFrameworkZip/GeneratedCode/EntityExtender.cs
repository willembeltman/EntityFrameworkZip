using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Helpers;

namespace EntityFrameworkZip.GeneratedCode;

public class EntityExtender<T>
{
    private Action<T, DbContext> ExtendEntityDelegate;
    public readonly string Code;

    public void ExtendEntity(T entity, DbContext dbContext)
    {
        ExtendEntityDelegate(entity, dbContext);
    }

    internal EntityExtender(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityExtender";
        var methodName = "ExtendEntity";

        Code = GenerateSerializerCode(type, className, methodName, dbContext);

        var asm = Compile(Code);
        var serializerType = asm.GetType(className)!;
        var createProxyMethod = serializerType.GetMethod(methodName)!;

        ExtendEntityDelegate = (Action<T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<T, DbContext>), createProxyMethod)!;
    }

    private string GenerateSerializerCode(Type type, string proxyName, string methodName, DbContext dbContext)
    {
        var className = type.Name;
        var fullClassName = type.FullName;

        var lazyCode = string.Empty;

        var foreignEntityCollectionType = typeof(LazyForeignEntityCollection<,>);
        var foreignEntityCollectionFullName = foreignEntityCollectionType.FullName!.Split('`').First();

        var entitySerializerType = typeof(EntitySerializer<>);
        var entitySerializerFullName = entitySerializerType.FullName!.Split('`').First();

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var entityExtenderCollectionType = typeof(EntityExtenderCollection);
        var entityExtenderCollectionTypeFullName = entityExtenderCollectionType.FullName;
        var entityExtenderCollectionTypeMethod = entityExtenderCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            var propertyName = prop.Name;
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            if (ReflectionHelper.HasExtendedPropertiesOrLists(prop.PropertyType))
            {
                lazyCode += $@"
                    var {propertyName}Extender = {entityExtenderCollectionTypeFullName}.{entityExtenderCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                    {propertyName}Extender.ExtendEntity(item.{propertyName}, db);";
                continue;
            }
            var test1 = ReflectionHelper.IsExtendedProperty(prop);
            var test2 = ReflectionHelper.IsExtendedList(prop);
            if (!(test1 || test2)) continue;

            if (ReflectionHelper.HasIEnumerableInterface(prop.PropertyType))
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
                    .FirstOrDefault(a => ReflectionHelper.GetDbSetType(a) == foreignType);

                if (foreignPropertyOnApplicationDbContext == null) 
                    throw new Exception($"ZipDatabase Exception: DbSet<{foreignType.Name}> not found on {applicationDbContextType.Name}.");

                if (!ReflectionHelper.HasIEntityInterface(type))
                    throw new Exception(
                        $"ZipDatabase Exception: Type '{type.FullName}' does not implement IEntity interface, though is used to filter in the " +
                        $"{foreignType.Name} entities with '{foreignKeyName}'. Type {type.Name} needs a primary key " +
                        $"('public long Id {{ get; set; }}' property) to filter in Entities (you can copy it from the " +
                        $"parent entity '{foreignType}').");

                var foreignPropertyOnApplicationDbContextName = foreignPropertyOnApplicationDbContext.Name; 

                lazyCode += $@"
                    if (item.{propertyName} != null && item.{propertyName}.GetType() != typeof({foreignEntityCollectionFullName}<{foreignType.FullName}, {fullClassName}>))
                    {{
                        foreach(var subitem in item.{propertyName})
                        {{
                            if (subitem.{foreignKeyName} != item.Id)
                                subitem.{foreignKeyName} = item.Id;
                            if (subitem.Id < 1)
                                db.{foreignPropertyOnApplicationDbContextName}.Add(subitem);
                        }}
                    }}

                    item.{propertyName} = new {foreignEntityCollectionFullName}<{foreignType.FullName}, {fullClassName}>(
                        db.{foreignPropertyOnApplicationDbContextName},
                        item,
                        (foreign, primary) => foreign.{foreignKeyName} == primary.Id,
                        (foreign, primary) => foreign.{foreignKeyName} = primary.Id);";
            }
            else if (ReflectionHelper.IsLazy(prop))
            {
                var foreignType = ReflectionHelper.GetLazyType(prop);
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

                lazyCode += @$"
                    item.{propertyName} = new Lazy<{foreignType.FullName}?>(() => 
                        {{
                            var subitem = db.{lazyPropertyOnApplicationDbContextName}.FirstOrDefault(t => t.Id == item.{foreignKeyName});
                            if (subitem == null) return null;
                            return subitem;
                        }});";
            }
        }

        return $@"
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public static class {proxyName}
            {{
                public static void {methodName}({fullClassName} item, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
            {lazyCode}
                }}
            }}";
    }

    public static string? GetCSharpTypeName(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(char)) return "char";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return GetCSharpTypeName(Nullable.GetUnderlyingType(type)!) + "?";

        return type.FullName; // fallback, e.g. for classes
    }

    private Assembly Compile(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "GeneratedEntityExtenders",
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
}