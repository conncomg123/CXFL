// utility class, simliar to the fl object in JSFL
using System.IO.Compression;

namespace CsXFL;
public static class An
{
    public static bool ImportFromOtherDocument(this Document doc, string otherDocPath, string itemName)
    {
        HashSet<string> filesToImport = new();
        bool isSymbol = Path.GetExtension(itemName) == ".xml", isFla = Path.GetExtension(otherDocPath) == ".fla";
        if (Path.GetExtension(itemName) == string.Empty)
        {
            itemName += ".xml";
            isSymbol = true;
        }
        if (isFla)
        {
            // extract fla to xfl in temp directory
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            while (Directory.Exists(tempDir))
            {
                tempDir = Path.Combine(tempDir, Path.GetRandomFileName());
            }
            Directory.CreateDirectory(tempDir);
            using (ZipArchive archive = ZipFile.Open(otherDocPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDir);
            }
            otherDocPath = tempDir;
        }
        string otherItemPath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, itemName);
        if (isSymbol)
        {
            AddSymbolAndDependencies(otherItemPath, filesToImport, otherDocPath);
        }
        filesToImport.Add(otherItemPath);
        foreach (string file in filesToImport)
        {
            if (!File.Exists(file)) return false;
            doc.Library.ImportItem(file, true);
        }
        if (isFla)
        {
            Directory.Delete(otherDocPath, true);
        }
        return true;
    }
    private static void AddSymbolAndDependencies(string symbolPath, HashSet<string> filesToImport, string otherDocPath)
    {
        string toImport = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, symbolPath);
        if(Path.GetExtension(toImport) == string.Empty) toImport += ".xml";
        if(!filesToImport.Add(toImport)) return;

        HashSet<string> dependencies = ParseSymbolFile(toImport, filesToImport);

        foreach (string dependency in dependencies)
        {
            AddSymbolAndDependencies(dependency, filesToImport, otherDocPath);
        }
    }
    private static HashSet<string> ParseSymbolFile(string symbolPath, HashSet<string> existingDependencies)
    {
        HashSet<string> dependencies = new();
        SymbolItem symbol = SymbolItem.FromFile(symbolPath);
        foreach(Layer l in symbol.Timeline.Layers)
        {
            foreach(Frame f in l.KeyFrames)
            {
                foreach(Element e in f.Elements)
                {
                    if (e is Instance i)
                    {
                        if (existingDependencies.Contains(i.LibraryItemName)) continue;
                        dependencies.Add(i.LibraryItemName);
                    }
                }
            }
        }
        return dependencies;
    }

}