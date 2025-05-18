using System.IO.Compression;

namespace EntityFrameworkZip.Extentions;

public static class ZipArchiveExtention
{
    public static ZipArchiveEntry GetOrCreateEntry(this ZipArchive zipArchive, string name)
    {
        var dataEntry = zipArchive.GetEntry(name);
        if (dataEntry == null)
            return zipArchive.CreateEntry(name);
        return dataEntry;
    }
}
