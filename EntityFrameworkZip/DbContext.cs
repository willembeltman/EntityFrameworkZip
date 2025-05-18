using System.IO.Compression;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip;

public class DbContext
{
    public DbContext(string fullName)
    {
        FullName = fullName;

        // Hou de zip open, zodat hij gelocked is
        using var ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
        using var ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

        DbSets = [];

        var extender = DbContextExtenderCollection.GetOrCreate(this);
        extender.ExtendDbContext(this, ZipArchive);
    }

    public string FullName { get; }


    internal List<IDbSet> DbSets;

    internal void AddDbSet(IDbSet dbSet)
    {
        DbSets.Add(dbSet);
    }

    public void SaveChanges()
    {
        if (File.Exists(FullName))
            File.Delete(FullName);

        using var ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
        using var ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

        foreach (var dbSet in DbSets)
            dbSet.WriteCache(ZipArchive);
    }
}
