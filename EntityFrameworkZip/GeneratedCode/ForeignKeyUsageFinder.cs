using Microsoft.CodeAnalysis;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Helpers;
using EntityFrameworkZip.ExtendedEntity;

namespace EntityFrameworkZip.GeneratedCode;

public class ForeignKeyUsageFinder<T> : CodeCompiler
{
    private readonly Func<T, DbContext, bool, bool> ExtendEntityDelegate;
    public readonly string Code;

    public bool FindForeignKeyUsage(T entity, DbContext dbContext, bool removeIfFound = false)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        return ExtendEntityDelegate(entity, dbContext, removeIfFound);
    }

    internal ForeignKeyUsageFinder(DbContext dbContext)
    {
        var type = typeof(T);
        var className = $"{type.Name}ForeignKeyUsageFinder";
        var methodName = "FindForeignKeyUsage";

        Code = GenerateFinderCode(type, className, methodName, dbContext);

        var asm = Compile(Code);
        var serializerType = asm.GetType(className)!;
        var createProxyMethod = serializerType.GetMethod(methodName)!;

        ExtendEntityDelegate = (Func<T, DbContext, bool, bool>)Delegate.CreateDelegate(
            typeof(Func<T, DbContext, bool, bool>), createProxyMethod)!;
    }

    private static string GenerateFinderCode(Type type, string proxyName, string methodName, DbContext dbContext)
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
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public static class {proxyName}
            {{
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
            }}";
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