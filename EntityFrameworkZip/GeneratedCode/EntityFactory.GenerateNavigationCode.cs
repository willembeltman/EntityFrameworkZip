using EntityFrameworkZip.Navigation;

namespace EntityFrameworkZip.GeneratedCode;

public partial class EntityFactory<T>
{
    private static string GenerateNavigationCode(Type type, string methodName, DbContext dbContext)
    {
        var className = type.Name;
        var fullClassName = type.FullName;

        var lazyCode = string.Empty;

        var foreignEntityCollectionNotNullType = typeof(LazyEntityCollectionNotNull<,>);
        var foreignEntityCollectionNotNullFullName = foreignEntityCollectionNotNullType.FullName!.Split('`').First();

        var foreignEntityCollectionNullType = typeof(LazyEntityCollectionNull<,>);
        var foreignEntityCollectionNullFullName = foreignEntityCollectionNullType.FullName!.Split('`').First();

        var foreignEntityLazyNotNullType = typeof(LazyEntityNotNull<,>);
        var foreignEntityLazyNotNullFullName = foreignEntityLazyNotNullType.FullName!.Split('`').First();

        var foreignEntityLazyNullType = typeof(LazyEntityNull<,>);
        var foreignEntityLazyNullFullName = foreignEntityLazyNullType.FullName!.Split('`').First();

        var entityFactoryType = typeof(EntityFactory<>);
        var entityFactoryFullName = entityFactoryType.FullName!.Split('`').First();

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var entityFactoryCollectionType = typeof(EntityFactoryCollection);
        var entityFactoryCollectionTypeFullName = entityFactoryCollectionType.FullName;
        var entityFactoryCollectionTypeMethod = entityFactoryCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            var propertyName = prop.Name;
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            if (ReflectionHelper.HasNavigationProperties(prop.PropertyType))
            {
                lazyCode += $@"
                    if (item.{propertyName} != null)
                    {{
                        var {propertyName}EntityFactory = {entityFactoryCollectionTypeFullName}.{entityFactoryCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
                        {propertyName}EntityFactory.SetNavigationProperties(item.{propertyName}, db);
                    }}";
                continue;
            }
            if (!ReflectionHelper.IsNavigationProperty(prop)) continue;

            if (ReflectionHelper.IsNavigationListProperty(prop))
            {
                GenerateNavigationCode_GenerateForeignListProperty(type, className, fullClassName, ref lazyCode, foreignEntityCollectionNotNullFullName, foreignEntityCollectionNullFullName, applicationDbContextType, prop, propertyName);
            }
            else if (ReflectionHelper.IsNavigationEntityProperty(prop))
            {
                GenerateNavigationCode_GenerateForeignEntityProperty(type, fullClassName, ref lazyCode, foreignEntityLazyNotNullFullName, foreignEntityLazyNullFullName, applicationDbContextType, prop, propertyName);
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
    private static void GenerateNavigationCode_GenerateForeignEntityProperty(
        Type type, string? fullClassName, ref string lazyCode,
        string foreignEntityLazyNotNullFullName, string foreignEntityLazyNullFullName,
        Type applicationDbContextType, System.Reflection.PropertyInfo prop, string propertyName)
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
        if (lazyPropertyOnApplicationDbContext == null) return;

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
    private static void GenerateNavigationCode_GenerateForeignListProperty(
        Type type, string className, string? fullClassName, ref string lazyCode,
        string foreignEntityCollectionNotNullFullName, string foreignEntityCollectionNullFullName,
        Type applicationDbContextType, System.Reflection.PropertyInfo prop, string propertyName)
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

        if (!ReflectionHelper.IsIEntity(type))
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
}
