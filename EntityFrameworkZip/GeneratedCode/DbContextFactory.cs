using EntityFrameworkZip;
using EntityFrameworkZip.GeneratedCode;
using System.IO.Compression;

public class DbContextFactory
{
    private readonly Action<DbContext, ZipArchive> LoadDbSetsFromZipDelegate;
    private readonly Action<DbContext, DirectoryInfo> LoadDbSetsFromDirectoryDelegate;

    /// <summary>
    /// The generated source code used to compile the DbContext extension logic.
    /// This is a string representation of C# code that initializes specific DbSets within the DbContext.
    /// </summary>
    public readonly string Code;

    /// <summary>
    /// Extends the given DbContext by initializing its DbSet collections with data from a ZipArchive.
    /// This is done using the dynamically compiled delegate <see cref="LoadDbSetsFromZipDelegate"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext to extend.</param>
    /// <param name="zipArchive">The ZipArchive from which data is loaded.</param>
    public void LoadDbSetsFromZip(DbContext dbContext, ZipArchive zipArchive)
    {
        LoadDbSetsFromZipDelegate(dbContext, zipArchive);
    }

    /// <summary>
    /// Extends the given DbContext by initializing its DbSet collections with data from a directory.
    /// This is done using the dynamically compiled delegate <see cref="LoadDbSetsFromDirectoryDelegate"/>.
    /// </summary>
    /// <param name="dbContext">The DbContext to extend.</param>
    /// <param name="directory">The directory from which data is loaded.</param>
    public void LoadDbSetsFromDirectory(DbContext dbContext, DirectoryInfo directory)
    {
        LoadDbSetsFromDirectoryDelegate(dbContext, directory);
    }

    /// <summary>
    /// Constructor that dynamically generates and compiles code to extend a DbContext.
    /// Based on reflection of the DbContext type, it generates C# code that initializes all DbSet properties
    /// with instances linked to either a ZipArchive or a directory.
    /// </summary>
    /// <param name="dbContext">The DbContext whose type is used for code generation.</param>
    public DbContextFactory(DbContext dbContext)
    {
        var applicationDbContextType = dbContext.GetType();
        var extenderName = $"{applicationDbContextType.Name}DbContextFactory";
        var extenderMethodNameZip = "ExtendDbContextZip";
        var extenderMethodNameDirectory = "ExtendDbContextDirectory";

        // Genereer de C# broncode die de extend-methodes definieert
        Code = GenerateDbSetLoaderCode(applicationDbContextType, extenderName, extenderMethodNameZip, extenderMethodNameDirectory);

        // Compileer de gegenereerde code in een assembly
        var asm = CodeCompiler.Compile(Code);

        // Haal het type van de extender class uit de assembly
        var serializerType = asm.GetType(extenderName)!;

        // Haal de methodes op die de DbContext extensies uitvoeren
        var createProxyMethodZip = serializerType.GetMethod(extenderMethodNameZip)!;
        var createProxyMethodDirectory = serializerType.GetMethod(extenderMethodNameDirectory)!;

        // Maak delegates aan voor de methodes zodat ze snel aangeroepen kunnen worden
        LoadDbSetsFromZipDelegate = (Action<DbContext, ZipArchive>)Delegate.CreateDelegate(
            typeof(Action<DbContext, ZipArchive>), createProxyMethodZip)!;
        LoadDbSetsFromDirectoryDelegate = (Action<DbContext, DirectoryInfo>)Delegate.CreateDelegate(
            typeof(Action<DbContext, DirectoryInfo>), createProxyMethodDirectory)!;
    }

    /// <summary>
    /// Generates the source code for an extender class that contains two methods:
    /// - ExtendDbContextZip: initializes DbSets using a ZipArchive
    /// - ExtendDbContextDirectory: initializes DbSets using a DirectoryInfo
    /// 
    /// For each public, settable DbSet property in the DbContext, code is generated that creates a new
    /// DbSet instance backed by either the zip archive or the directory.
    /// </summary>
    /// <param name="applicationDbContextType">The type of the DbContext.</param>
    /// <param name="extenderName">The name of the generated extender class.</param>
    /// <param name="extenderZipMethodName">The name of the method that uses a ZipArchive.</param>
    /// <param name="extenderDirectoryMethodName">The name of the method that uses a DirectoryInfo.</param>
    /// <returns>The complete generated source code as a string.</returns>
    private static string GenerateDbSetLoaderCode(Type applicationDbContextType, string extenderName, string extenderZipMethodName, string extenderDirectoryMethodName)
    {
        var applicationDbContextName = applicationDbContextType.Name;
        var applicationDbContextFullName = applicationDbContextType.FullName;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var dbSetType = typeof(DbSet<>);
        var dbSetFullName = dbSetType.FullName!.Split('`').First();

        var propertiesZipCode = string.Empty;
        var propertiesDirecoryCode = string.Empty;
        var props = applicationDbContextType.GetProperties();
        foreach (var property in props)
        {
            if (!ReflectionHelper.HasPublicGetter(property)) continue;
            if (!ReflectionHelper.HasPublicSetter(property)) continue;
            if (!ReflectionHelper.IsDbSet(property)) continue;

            var propertyName = property.Name;

            var propertyType = ReflectionHelper.GetDbSetType(property);
            var propertyTypeName = propertyType.Name;
            var propertyTypeFullName = propertyType.FullName;

            propertiesZipCode += $@"
                    db.{propertyName} = new {dbSetFullName}<{propertyTypeFullName}>(db, zipArchive);";
            propertiesDirecoryCode += $@"
                    db.{propertyName} = new {dbSetFullName}<{propertyTypeFullName}>(db, directory);";
        }

        return $@"
            using System;

            public static class {extenderName}
            {{
                public static void {extenderZipMethodName}({dbContextTypeFullName} dbContext, System.IO.Compression.ZipArchive zipArchive)
                {{
                    var db = dbContext as {applicationDbContextFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextName}"");
                    {propertiesZipCode}
                }}
                public static void {extenderDirectoryMethodName}({dbContextTypeFullName} dbContext, System.IO.DirectoryInfo directory)
                {{
                    var db = dbContext as {applicationDbContextFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextName}"");
                    {propertiesDirecoryCode}
                }}
            }}";
    }
}
