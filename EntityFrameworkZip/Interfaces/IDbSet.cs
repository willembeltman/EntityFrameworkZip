using System.IO.Compression;

namespace EntityFrameworkZip.Interfaces;

internal interface IDbSet
{
    string TypeName { get; }

    void LoadCache(ZipArchive zipArchive);
    void WriteCache(ZipArchive zipArchive);
}