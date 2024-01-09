using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using CsXFL;
public class Program
{
    public static void Main(string[] args)
    {
        CsXFL.Document doc = new("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\DOMDocument.xml");
        Console.WriteLine((doc.Library.Items["larger.png"] as BitmapItem)!.CompressionType);
        // doc.Save("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\test.fla");
    }
}