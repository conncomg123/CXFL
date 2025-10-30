using Scripts;
using System.IO;
using System.IO.Compression;

namespace Rendering;
class Program
{
    static void Main(string[] args)
    {
        using MemoryStream memoryStream = new MemoryStream();

        ZipArchive zipArchive = ZipArchiveBuilder.CreateNewDocument(1920, 1080, 23.98, memoryStream);
        zipArchive.Dispose();

        string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Untitled-1.fla");

        using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        memoryStream.Position = 0;
        memoryStream.CopyTo(fileStream);

        Console.WriteLine($"Untitled-1.fla has been saved to: {outputPath}");
    }
}