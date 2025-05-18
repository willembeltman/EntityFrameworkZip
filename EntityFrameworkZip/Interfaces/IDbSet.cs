using System.IO.Compression;

namespace EntityFrameworkZip.Interfaces;

public interface IDbSet
{
    string TypeName { get; }

    void LoadCache(ZipArchive zipArchive);
    void WriteCache(ZipArchive zipArchive);
}