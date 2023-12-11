using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;

public class Program
{
    public static void Main(string[] args)
    {
        CsXFL.Document doc = new("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\test.fla");
        System.Console.WriteLine(doc.GetTimeline(0).GetLayerCount());
        doc.GetTimeline(0).DeleteLayer(4);
        System.Console.WriteLine(doc.GetTimeline(0).GetLayerCount());
        // doc.Save("C:\\Stuff\\CXFL\\CsXFL.Tests\\TestAssets\\test.fla");
    }
}