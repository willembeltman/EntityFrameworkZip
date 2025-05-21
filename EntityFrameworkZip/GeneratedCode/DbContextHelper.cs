using EntityFrameworkZip.GeneratedCode;
using EntityFrameworkZip.Helpers;
using EntityFrameworkZip;
using System.IO.Compression;

public class DbContextHelper : CodeCompiler
{
    /// <summary>
    /// Delegate die de logica bevat om een DbContext uit te breiden met DbSet-instanties
    /// vanuit een ZipArchive (gecomprimeerde opslag).
    /// </summary>
    private readonly Action<DbContext, ZipArchive> ExtendDbContextZipDelegate;

    /// <summary>
    /// Delegate die de logica bevat om een DbContext uit te breiden met DbSet-instanties
    /// vanuit een directory (map in het bestandssysteem).
    /// </summary>
    private readonly Action<DbContext, DirectoryInfo> ExtendDbContextDirectoryDelegate;

    /// <summary>
    /// De gegenereerde broncode die gebruikt wordt om de extensies te compileren.
    /// Dit is een stringrepresentatie van C#-code die specifieke DbSets in de DbContext initialiseert.
    /// </summary>
    public readonly string Code;

    /// <summary>
    /// Breidt de gegeven DbContext uit door de DbSet-collecties te initialiseren met data uit een ZipArchive.
    /// Dit wordt gedaan via de dynamisch gecompileerde delegate <see cref="ExtendDbContextZipDelegate"/>.
    /// </summary>
    /// <param name="dbContext">De DbContext die uitgebreid wordt.</param>
    /// <param name="zipArchive">De ZipArchive waaruit de data wordt geladen.</param>
    public void ExtendDbContext(DbContext dbContext, ZipArchive zipArchive)
    {
        ExtendDbContextZipDelegate(dbContext, zipArchive);
    }

    /// <summary>
    /// Breidt de gegeven DbContext uit door de DbSet-collecties te initialiseren met data uit een directory.
    /// Dit wordt gedaan via de dynamisch gecompileerde delegate <see cref="ExtendDbContextDirectoryDelegate"/>.
    /// </summary>
    /// <param name="dbContext">De DbContext die uitgebreid wordt.</param>
    /// <param name="directory">De directory waaruit de data wordt geladen.</param>
    public void ExtendDbContext(DbContext dbContext, DirectoryInfo directory)
    {
        ExtendDbContextDirectoryDelegate(dbContext, directory);
    }

    /// <summary>
    /// Constructor die dynamisch code genereert en compileert om de DbContext uit te breiden.
    /// Op basis van de reflectie op het DbContext-type wordt C# code gegenereerd die alle DbSet properties
    /// initialiseert met DbSet instanties die gekoppeld zijn aan een ZipArchive of directory.
    /// </summary>
    /// <param name="dbContext">De DbContext waarvan het type wordt gebruikt voor codegeneratie.</param>
    public DbContextHelper(DbContext dbContext)
    {
        var applicationDbContextType = dbContext.GetType();
        var extenderName = $"{applicationDbContextType.Name}DbContextHelper";
        var extenderMethodNameZip = "ExtendDbContextZip";
        var extenderMethodNameDirectory = "ExtendDbContextDirectory";

        // Genereer de C# broncode die de extend-methodes definieert
        Code = GenerateSerializerCode(applicationDbContextType, extenderName, extenderMethodNameZip, extenderMethodNameDirectory);

        // Compileer de gegenereerde code in een assembly
        var asm = Compile(Code);

        // Haal het type van de extender class uit de assembly
        var serializerType = asm.GetType(extenderName)!;

        // Haal de methodes op die de DbContext extensies uitvoeren
        var createProxyMethodZip = serializerType.GetMethod(extenderMethodNameZip)!;
        var createProxyMethodDirectory = serializerType.GetMethod(extenderMethodNameDirectory)!;

        // Maak delegates aan voor de methodes zodat ze snel aangeroepen kunnen worden
        ExtendDbContextZipDelegate = (Action<DbContext, ZipArchive>)Delegate.CreateDelegate(
            typeof(Action<DbContext, ZipArchive>), createProxyMethodZip)!;
        ExtendDbContextDirectoryDelegate = (Action<DbContext, DirectoryInfo>)Delegate.CreateDelegate(
            typeof(Action<DbContext, DirectoryInfo>), createProxyMethodDirectory)!;
    }

    /// <summary>
    /// Genereert de broncode voor een extender klasse die twee methoden bevat:
    /// - ExtendDbContextZip: initialiseert DbSets met ZipArchive
    /// - ExtendDbContextDirectory: initialiseert DbSets met een DirectoryInfo
    /// 
    /// Voor elke publieke, setbare DbSet property in de DbContext wordt code gegenereerd die een nieuwe DbSet instantie
    /// maakt met de corresponderende opslag (zip of directory).
    /// </summary>
    /// <param name="applicationDbContextType">Het type van de DbContext.</param>
    /// <param name="extenderName">De naam van de gegenereerde extender klasse.</param>
    /// <param name="extenderZipMethodName">De naam van de methode die een ZipArchive gebruikt.</param>
    /// <param name="extenderDirectoryMethodName">De naam van de methode die een DirectoryInfo gebruikt.</param>
    /// <returns>De volledige broncode als string.</returns>
    private static string GenerateSerializerCode(Type applicationDbContextType, string extenderName, string extenderZipMethodName, string extenderDirectoryMethodName)
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
