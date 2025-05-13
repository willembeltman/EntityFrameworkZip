using System.IO.Compression;
using EntityFrameworkZip.Collections;
using EntityFrameworkZip.Interfaces;

namespace EntityFrameworkZip;

public class DbContext : IDisposable
{
    public DbContext(string fullName)
    {
        FullName = fullName;

        // Hou de zip open, zodat hij gelocked is
        ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
        ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

        DbSets = new List<IDbSet>();

        var extender = DbContextExtenderCollection.GetOrCreate(this);
        extender.ExtendDbContext(this);
    }

    public string FullName { get; }

    internal Stream ZipStream { get; private set; }
    internal ZipArchive ZipArchive { get; private set; }

    internal List<IDbSet> DbSets;

    internal void AddDbSet(IDbSet dbSet)
    {
        DbSets.Add(dbSet);
    }

    public void SaveChanges()
    {
        ZipArchive.Dispose();
        ZipStream.Dispose();

        if (File.Exists(FullName))
            File.Delete(FullName);

        ZipStream = File.Open(FullName!, FileMode.OpenOrCreate);
        ZipArchive = new ZipArchive(ZipStream, ZipArchiveMode.Update);

        foreach (var dbSet in DbSets)
            dbSet.WriteCache(ZipArchive);
    }
    public void Dispose()
    {
        ZipArchive.Dispose();
        ZipStream.Dispose();
        GC.SuppressFinalize(this);
    }
}
