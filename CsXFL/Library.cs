using System.Xml.Linq;

namespace CsXFL;
public class Library
{
    public const string LIBRARY_PATH = "LIBRARY";
    private readonly Dictionary<string, Item> items;
    private readonly List<Item> unusedItems;
    private readonly Document containingDocument;
    public Dictionary<string, Item> Items { get { return items; } }
    public List<Item> UnusedItems { get { return unusedItems; } }
    private void LoadXFLFolders(XElement foldersNode)
    {
        List<XElement>? folderNodes = foldersNode.Elements().ToList();
        if (folderNodes is null) return;
        foreach (XElement folderNode in folderNodes)
        {
            FolderItem folder = new(folderNode);
            items.Add(folder.Name, folder);
        }
    }
    private void LoadXFLMedia(XElement mediaNode)
    {
        List<XElement>? mediaNodes = mediaNode.Elements().ToList();
        if (mediaNodes is null) return;
        foreach (XElement mediaNode_ in mediaNodes)
        {
            string mediaType = mediaNode_.Name.LocalName;
            switch (mediaType)
            {
                case "DOMBitmapItem":
                    BitmapItem bitmap = new(mediaNode_);
                    items.Add(bitmap.Name, bitmap);
                    break;
                case "DOMSoundItem":
                    SoundItem sound = new(mediaNode_);
                    items.Add(sound.Name, sound);
                    break;
            }
        }
    }
    private void LoadXFLSymbols(XElement symbolsNode)
    {
        List<XElement>? symbolNodes = symbolsNode.Elements().ToList();
        if (symbolNodes is null) return;
        foreach (XElement symbolNode in symbolNodes)
        {
            string symbolPath = (string)symbolNode.Attribute("href")!;
            if (symbolPath == string.Empty) continue;
            // make symbolPath into absolute path
            symbolPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, symbolPath);
            XDocument? symbolTree = XDocument.Load(symbolPath);
            if (symbolTree is null) continue;
            SymbolItem symbol = new(symbolTree.Root!);
            items.Add(symbol.Name, symbol);
        }
    }
    private void LoadFLALibrary(XElement documentNode)
    {

    }
    private void LoadXFLLibrary(XElement documentNode)
    {
        // Everything before the first timeline is in the library
        List<XElement>? libraryNodes = documentNode.Elements().ToList();
        if (libraryNodes is null) return;
        foreach (XElement libraryNode in libraryNodes)
        {
            string currentNode = libraryNode.Name.LocalName;
            if (currentNode == "timelines") break;
            switch (currentNode)
            {
                case "folders":
                    LoadXFLFolders(libraryNode);
                    break;
                case "media":
                    LoadXFLMedia(libraryNode);
                    break;
                case "symbols":
                    LoadXFLSymbols(libraryNode);
                    break;

            }
        }
    }
    public Library(in Document containingDocument)
    {
        items = new Dictionary<string, Item>();
        unusedItems = new List<Item>();
        this.containingDocument = containingDocument;
        if (containingDocument.IsXFL)
        {
            LoadXFLLibrary(containingDocument.Root!);
        }
        else
        {
            LoadFLALibrary(containingDocument.Root!);
        }
    }
    public bool ItemExists(string namePath)
    {
        return items.ContainsKey(namePath);
    }
    public bool AddItemToDocument(double posX, double posY, string namePath, Frame? where = null)
    {
        if (where is null)
        {
            var currentTimeline = containingDocument.GetTimeline(containingDocument.CurrentTimeline);
            var currentLayer = currentTimeline.Layers[currentTimeline.CurrentLayer];
            var currentFrame = currentLayer.GetFrame(currentTimeline.CurrentFrame);
            where = currentFrame;
        }
        if (!ItemExists(namePath)) return false;
        Item item = items[namePath];
        if (item is null) return false;
        var added = where.AddItem(item);
        if (added != null)
        {
            added.Matrix.Tx = posX;
            added.Matrix.Ty = posY;
        }
        return true;
    }
}