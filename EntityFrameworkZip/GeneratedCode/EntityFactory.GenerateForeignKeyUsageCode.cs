namespace EntityFrameworkZip.GeneratedCode;

public partial class EntityFactory<T> 
{
    private static string GenerateForeignKeyUsageCode(Type type, string methodName, DbContext dbContext)
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

        if (ReflectionHelper.IsIEntity(type))
        {
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
                GenerateForeignKeyUsageCode_GenerateForEntity(type, entityType, dbSetName, applicationDbContextTypeFullName,
                    ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex);
            }
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
    private static void GenerateForeignKeyUsageCode_GenerateForEntity(
        Type type, Type entityType, string dbSetName, string applicationDbContextTypeFullName,
        ref string codeRemoveIfFound, ref string codeExceptionIfFound, ref int listIndex,
        List<Type>? doneTypes = null, string baseProperty = "", string basePropertyNull = "")
    {
        if (!ReflectionHelper.IsIEntity(type)) return;

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
            if (ReflectionHelper.HasNavigationProperties(entityProp.PropertyType) &&
                ReflectionHelper.IsIEntity(entityProp.PropertyType))
            {
                GenerateForeignKeyUsageCode_GenerateForEntity(type, entityProp.PropertyType, dbSetName, applicationDbContextTypeFullName,
                    ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex,
                    doneTypes, baseProperty + $".{entityProp.Name}", basePropertyNull + $".{entityProp.Name}?");
                continue;
            }
            if (!ReflectionHelper.IsNavigationEntityProperty(entityProp)) continue;

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
                        var list{dbSetName}{listIndex} = db.{dbSetName}.Where(a => a{basePropertyNull}.{foreignKeyName} == item.Id);
                        foreach (var item{listIndex} in list{dbSetName}{listIndex})
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
