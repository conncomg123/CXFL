// utility class, simliar to the fl object in JSFL
using System.IO.Compression;

namespace CsXFL;
public static class An
{
    private static Dictionary<string, string> extractedFlas = new();
    private static List<Document> _documents = new();
    static An()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
    }
    public static async Task<Document> OpenDocumentAsync(string filepath)
    {
        Document doc = await Task.Run(() => new Document(filepath));
        _documents.Add(doc);
        return doc;
    }
    public static Document GetDocument(int index)
    {
        return _documents[index];
    }
    public static void CloseDocument(int index)
    {
        _documents.RemoveAt(index);
    }
    public static void ImportFromOtherDocument(this Document doc, string otherDocPath, string itemName)
    {
        if (!File.Exists(otherDocPath)) throw new FileNotFoundException("The file does not exist", otherDocPath);
        HashSet<string> filesToImport = new();
        bool isSymbol = Path.GetExtension(itemName) == ".xml", isFla = Path.GetExtension(otherDocPath) == ".fla";
        if (Path.GetExtension(itemName) == string.Empty)
        {
            itemName += ".xml";
            isSymbol = true;
        }
        if (isFla)
        {
            if (!extractedFlas.TryGetValue(otherDocPath, out string? tempDir))
            {
                // extract fla to xfl in temp directory
                tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                while (Directory.Exists(tempDir))
                {
                    tempDir = Path.Combine(tempDir, Path.GetRandomFileName());
                }
                Directory.CreateDirectory(tempDir);
                using (ZipArchive archive = ZipFile.Open(otherDocPath, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempDir);
                }
                extractedFlas.TryAdd(otherDocPath, tempDir);
            }
            otherDocPath = tempDir + "/DOMDocument.xml";
        }
        string otherItemPath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, itemName);
        if (!File.Exists(otherItemPath)) throw new FileNotFoundException("The item does not exist in the other document", otherItemPath);
        if (isSymbol)
        {
            AddSymbolAndDependencies(otherItemPath, filesToImport, otherDocPath);
        }
        filesToImport.Add(otherItemPath);
        string otherDocumentLibraryRoot = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH).Replace('\\', '/');
        foreach (string file in filesToImport)
        {
            doc.Library.ImportItem(file, true, otherDocumentLibraryRoot);
        }
    }
    private static void AddSymbolAndDependencies(string symbolPath, HashSet<string> filesToImport, string otherDocPath)
    {
        string toImport = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, symbolPath).Replace('\\', '/');
        if (Path.GetExtension(toImport) == string.Empty) toImport += ".xml";
        if (!filesToImport.Add(toImport)) return;
        if (Path.GetExtension(toImport) != ".xml") return; // not a symbol

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
        foreach (Layer l in symbol.Timeline.Layers)
        {
            foreach (Frame f in l.KeyFrames)
            {
                foreach (Element e in f.Elements)
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
    public static void ImportFolderFromOtherDocument(this Document doc, string otherDocPath, string folderName)
    {
        if (!File.Exists(otherDocPath)) throw new FileNotFoundException("The file does not exist", otherDocPath);
        bool isFla = Path.GetExtension(otherDocPath) == ".fla";
        if (isFla)
        {
            if (!extractedFlas.TryGetValue(otherDocPath, out string? tempDir))
            {
                // extract fla to xfl in temp directory
                tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                while (Directory.Exists(tempDir))
                {
                    tempDir = Path.Combine(tempDir, Path.GetRandomFileName());
                }
                Directory.CreateDirectory(tempDir);
                using (ZipArchive archive = ZipFile.Open(otherDocPath, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempDir);
                }
                extractedFlas.TryAdd(otherDocPath, tempDir);
            }
            otherDocPath = tempDir + "/DOMDocument.xml";
        }
        string otherFolderPath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, folderName);
        if (!Directory.Exists(otherFolderPath)) throw new DirectoryNotFoundException($"The folder does not exist in the other document: {folderName}");
        HashSet<string> filesToImport = new();
        foreach (string file in Directory.EnumerateFiles(otherFolderPath, "*", SearchOption.AllDirectories))
        {
            string fileWithoutBackslashes = file.Replace('\\', '/');
            bool isSymbol = Path.GetExtension(fileWithoutBackslashes) == ".xml";
            if (isSymbol)
            {
                AddSymbolAndDependencies(fileWithoutBackslashes, filesToImport, otherDocPath);
            }
            filesToImport.Add(fileWithoutBackslashes);
        }
        string otherDocumentLibraryRoot = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH).Replace('\\', '/');
        foreach (string file in filesToImport)
        {
            doc.Library.ImportItem(file, true, otherDocumentLibraryRoot);
        }
    }
    public static void Cleanup()
    {
        foreach (string tempDir in extractedFlas.Values)
        {
            Directory.Delete(tempDir, true);
        }
        extractedFlas.Clear();
    }
}