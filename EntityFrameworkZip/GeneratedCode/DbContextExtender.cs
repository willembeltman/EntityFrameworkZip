using EntityFrameworkZip.Helpers;
using System.IO.Compression;

namespace EntityFrameworkZip.GeneratedCode;

internal class DbContextExtender : CodeCompiler
{
    private readonly Action<DbContext, ZipArchive> ExtendDbContextDelegate;
    public readonly string Code;

    public void ExtendDbContext(DbContext dbContext, ZipArchive zipArchive)
    {
        ExtendDbContextDelegate(dbContext, zipArchive);
    }

    internal DbContextExtender(DbContext dbContext)
    {
        var applicationDbContextType = dbContext.GetType(); 
        var extenderName = $"{applicationDbContextType.Name}DbContextExtender";
        var extenderMethodName = "ExtendDbContext";

        Code = GenerateSerializerCode(applicationDbContextType, extenderName, extenderMethodName);

        var asm = Compile(Code);
        var serializerType = asm.GetType(extenderName)!;
        var createProxyMethod = serializerType.GetMethod(extenderMethodName)!;

        ExtendDbContextDelegate = (Action<DbContext, ZipArchive>)Delegate.CreateDelegate(
            typeof(Action<DbContext, ZipArchive>), createProxyMethod)!;
    }

    private static string GenerateSerializerCode(Type applicationDbContextType, string extenderName, string extenderMethodName)
    {
        var applicationDbContextName = applicationDbContextType.Name;
        var applicationDbContextFullName = applicationDbContextType.FullName;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var dbSetType = typeof(DbSet<>);
        var dbSetFullName = dbSetType.FullName!.Split('`').First();

        var propertiesCode = string.Empty;
        var props = applicationDbContextType.GetProperties();
        foreach (var property in props)
        {
            if (!ReflectionHelper.HasPublicGetter(property)) continue;
            if (!ReflectionHelper.HasPublicSetter(property)) continue;
            //if (!ReflectionHelper.IsVirtual(property)) continue; // I'll let this one slide
            if (!ReflectionHelper.IsDbSet(property)) continue;

            var propertyName = property.Name;

            var propertyType = ReflectionHelper.GetDbSetType(property);
            var propertyTypeName = propertyType.Name;
            var propertyTypeFullName = propertyType.FullName;

            propertiesCode += $@"
                    db.{propertyName} = new {dbSetFullName}<{propertyTypeFullName}>(db, zipArchive);";
        }

        return $@"
            using System;

            public static class {extenderName}
            {{
                public static void {extenderMethodName}({dbContextTypeFullName} dbContext, System.IO.Compression.ZipArchive zipArchive)
                {{
                    var db = dbContext as {applicationDbContextFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextName}"");
                    {propertiesCode}
                }}
            }}";
    }
}