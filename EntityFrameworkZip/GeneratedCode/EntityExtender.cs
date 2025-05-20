using Microsoft.CodeAnalysis;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Helpers;
using EntityFrameworkZip.ExtendedEntity;

namespace EntityFrameworkZip.GeneratedCode;

public class EntityExtender<T> : CodeCompiler
{
    private readonly Action<T, DbContext> ExtendEntityDelegate;
    public readonly string Code;

    public void ExtendEntity(T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        ExtendEntityDelegate(entity, dbContext);
    }

    internal EntityExtender(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityExtender";
        var methodName = "ExtendEntity";

        Code = GenerateExtenderCode(type, className, methodName, dbContext);

        var asm = Compile(Code);
        var serializerType = asm.GetType(className)!;
        var createProxyMethod = serializerType.GetMethod(methodName)!;

        ExtendEntityDelegate = (Action<T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<T, DbContext>), createProxyMethod)!;
    }

    private static string GenerateExtenderCode(Type type, string proxyName, string methodName, DbContext dbContext)
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
            if (ReflectionHelper.HasExtendedForeignProperties(prop.PropertyType))
            {
                lazyCode += $@"
                    if (item.{propertyName} != null)
                    {{
                        var {propertyName}Extender = {entityExtenderCollectionTypeFullName}.{entityExtenderCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
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
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public static class {proxyName}
            {{
                public static void {methodName}({fullClassName} item, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextTypeFullName}"");
                    {lazyCode}
                }}
            }}";
    }
}