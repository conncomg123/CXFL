using System.IO.Compression;
using System.Xml.Linq;

namespace CsXFL;
public class Library
{
    private class ItemOperation
    {
        public enum OperationType
        {
            Add,
            Remove,
            Rename
        }

        public OperationType Type { get; }
        public Item item { get; }
        public string ItemName { get; }
        public string? NewItemPath { get; }  // Only used for Add operations
        public string? NewItemName { get; }  // Only used for Rename operations

        public ItemOperation(Item item, OperationType type, string itemName, string? newItemPath = null, string? newItemName = null)
        {
            this.item = item;
            Type = type;
            ItemName = itemName;
            NewItemPath = newItemPath;
            NewItemName = newItemName;
        }
    }
    public const string LIBRARY_PATH = "LIBRARY", BINARY_PATH = "bin";
    private const int WAV_HEADER_SIZE = 44;
    private static readonly HashSet<string> SYMBOL_FILE_EXTENSIONS = new() { ".xml" }, AUDIO_FILE_EXTENSIONS = new() { ".mp3", ".wav", ".flac" }, IMAGE_FILE_EXTENSIONS = new() { ".png", ".jpg", ".jpeg", ".gif" };
    private readonly Dictionary<string, Item> items;
    private readonly Queue<ItemOperation> itemOperations;
    private readonly List<Item> unusedItems;
    private readonly Document containingDocument;
    private readonly XNamespace ns;
    public Dictionary<string, Item> Items { get { return items; } }
    public List<Item> UnusedItems { get { return unusedItems; } }
    private void LoadFolders(XElement foldersNode)
    {
        List<XElement>? folderNodes = foldersNode.Elements().ToList();
        if (folderNodes is null) return;
        foreach (XElement folderNode in folderNodes)
        {
            FolderItem folder = new(folderNode);
            items.Add(folder.Name, folder);
        }
    }
    private void LoadMedia(XElement mediaNode)
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
        foreach (XElement includeNode in symbolNodes)
        {
            string symbolPath = (string)includeNode.Attribute("href")!;
            if (symbolPath == string.Empty) continue;
            // make symbolPath into absolute path
            symbolPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, symbolPath);
            XDocument? symbolTree = XDocument.Load(symbolPath);
            if (symbolTree is null) continue;
            SymbolItem symbol = new(symbolTree.Root!, includeNode);
            items.Add(symbol.Name, symbol);
        }
    }
    private void LoadFLAFolders(XElement foldersNode)
    {
        List<XElement>? folderNodes = foldersNode.Elements().ToList();
        if (folderNodes is null) return;
        foreach (XElement folderNode in folderNodes)
        {
            FolderItem folder = new(folderNode);
            items.Add(folder.Name, folder);
        }
    }
    private void LoadFLAMedia(XElement mediaNode)
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
    private void LoadFLASymbols(XElement symbolsNode)
    {
        List<XElement>? symbolNodes = symbolsNode.Elements().ToList();
        if (symbolNodes is null) return;
        foreach (XElement includeNode in symbolNodes)
        {
            using ZipArchive archive = ZipFile.OpenRead(containingDocument.Filename);
            string symbolPath = (string)includeNode.Attribute("href")!;
            if (symbolPath == string.Empty) continue;
            // make symbolPath into relative path to the fla archive
            symbolPath = Path.Combine(LIBRARY_PATH, symbolPath);
            ZipArchiveEntry? symbolEntry = archive.GetEntry(symbolPath);
            if (symbolEntry is null) continue;
            XDocument? symbolTree = XDocument.Load(symbolEntry!.Open());
            if (symbolTree is null) continue;
            SymbolItem symbol = new(symbolTree.Root!, includeNode);
            items.Add(symbol.Name, symbol);
        }
    }
    private void LoadFLALibrary(XElement documentNode)
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
                    LoadFolders(libraryNode);
                    break;
                case "media":
                    LoadMedia(libraryNode);
                    break;
                case "symbols":
                    LoadFLASymbols(libraryNode);
                    break;

            }
        }
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
                    LoadFolders(libraryNode);
                    break;
                case "media":
                    LoadMedia(libraryNode);
                    break;
                case "symbols":
                    LoadXFLSymbols(libraryNode);
                    break;

            }
        }
    }
    internal Library(in Document containingDocument, XNamespace ns)
    {
        items = new Dictionary<string, Item>();
        unusedItems = new List<Item>();
        itemOperations = new();
        this.containingDocument = containingDocument;
        this.ns = ns;
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
    public bool AddItemToDocument(string namePath, double posX = 0, double posY = 0, Frame? where = null)
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
    internal Item? ImportItem(string path)
    {
        // todo: create an XElement node from the path, construct an Item from that, then enqueue an Add operation
        if (!File.Exists(path)) return null;
        string itemName = Path.GetFileName(path);
        string targetPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, itemName);
        if (File.Exists(targetPath)) return null;
        Item? imported = null;
        if (SYMBOL_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            // symbolitems are special as they can depend on other items in the library, so we need to iterate through its timeline, find all dependencies, and add them to the library
            imported = SymbolItem.FromFile(path);
            containingDocument.Root!.Element(ns + "symbols")!.Add((imported as SymbolItem)!.Include.Root);
        }
        else if (AUDIO_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            imported = SoundItem.FromFile(path, ns);
            containingDocument.Root!.Element(ns + "media")!.Add(imported.Root);
        }
        else if (IMAGE_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            imported = BitmapItem.FromFile(path, ns);
            containingDocument.Root!.Element(ns + "media")!.Add(imported.Root);
        }
        if (imported is null) return null;
        items.Add(itemName, imported);
        itemOperations.Enqueue(new ItemOperation(imported, ItemOperation.OperationType.Add, itemName, path));
        return imported;
    }
    public bool RenameItem(string oldName, string newName)
    {
        if (!ItemExists(oldName)) return false;
        if (ItemExists(newName)) return false;
        Item item = items[oldName];
        bool isSymbol = false;
        if (item is SymbolItem symbol)
        {
            symbol.Include.Href = newName + ".xml";
            symbol.Timeline.Name = newName;
            isSymbol = true;
        }
        if (item is SoundItem sound)
        {
            sound.Href = newName;
        }
        if (item is BitmapItem bitmap)
        {
            bitmap.Href = newName;
        }
        item.Name = newName;
        items.Remove(oldName);
        items.Add(newName, item);
        itemOperations.Enqueue(new ItemOperation(item, ItemOperation.OperationType.Rename, oldName + (isSymbol ? ".xml" : ""), null, newName + (isSymbol ? ".xml" : "")));
        LibraryEventMessenger.Instance.NotifyItemRenamed(oldName, newName);
        return true;
    }
    public bool RemoveItem(string itemPath)
    {
        if (!ItemExists(itemPath)) return false;
        Item item = items[itemPath];
        if (item is SymbolItem symbolItem)
        {
            symbolItem.Include.Root?.Remove();
        }
        item.Root?.Remove();
        items.Remove(itemPath);
        itemOperations.Enqueue(new ItemOperation(item, ItemOperation.OperationType.Remove, itemPath));
        LibraryEventMessenger.Instance.NotifyItemRemoved(itemPath);
        return true;
    }
    internal void Save(string filename)
    {
        ProcessItemOperations(filename);
        SaveSymbolItems(filename);
    }

    private void ProcessItemOperations(string filename)
    {
        while (itemOperations.Count > 0 && itemOperations.Dequeue() is ItemOperation operation)
        {
            string targetPath = Path.Combine(Path.GetDirectoryName(filename)!, LIBRARY_PATH, operation.ItemName);
            switch (operation.Type)
            {
                case ItemOperation.OperationType.Add:
                    ProcessAddOperation(operation, targetPath, filename);
                    break;
                case ItemOperation.OperationType.Remove:
                    ProcessRemoveOperation(operation, targetPath, filename);
                    break;
                case ItemOperation.OperationType.Rename:
                    ProcessRenameOperation(operation, targetPath);
                    break;
            }
        }
    }

    private void ProcessAddOperation(ItemOperation operation, string targetPath, string filename)
    {
        File.Copy(operation.NewItemPath!, targetPath);
        // update item's href
        Item item = operation.item;
        if (item is SymbolItem symbol)
        {
            symbol.Include.Href = operation.ItemName + ".xml";
        }
        if (item is SoundItem sound)
        {
            ProcessSoundItemAdd(sound, targetPath, filename, operation.ItemName);
        }
        if (item is BitmapItem bitmap)
        {
            bitmap.Href = operation.ItemName;
        }
    }

    private void ProcessSoundItemAdd(SoundItem sound, string targetPath, string filename, string itemName)
    {
        sound.Href = itemName;
        if (sound.Name.EndsWith(".flac"))
        {
            // Animate converts flac files to wav and removes their header, then puts it in the bin folder. Really weird but we gotta do the same thing.
            ArraySegment<byte> wavData = SoundUtils.ConvertFlacToWav(targetPath);
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int uniqueIndex = 0;
            string datFileName = "M " + uniqueIndex + " " + unixTime + ".dat";
            string wavPath = Path.Combine(Path.GetDirectoryName(filename)!, BINARY_PATH, datFileName);
            while(File.Exists(wavPath))
            {
                uniqueIndex++;
                datFileName = "M " + uniqueIndex + " " + unixTime + ".dat";
                wavPath = Path.Combine(Path.GetDirectoryName(filename)!, BINARY_PATH, datFileName);
            }
            using FileStream fs = new(wavPath, FileMode.Create);
            fs.Write(wavData.Array!, WAV_HEADER_SIZE, wavData.Count - WAV_HEADER_SIZE);
            sound.SoundDataHRef = datFileName;
        }
    }

    private void ProcessRemoveOperation(ItemOperation operation, string targetPath, string filename)
    {
        SoundItem? soundItem = operation.item as SoundItem;
        if (soundItem is not null)
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(filename)!, BINARY_PATH, soundItem.SoundDataHRef));
        }
        File.Delete(targetPath);
    }

    private void ProcessRenameOperation(ItemOperation operation, string targetPath)
    {
        string renamedPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, operation.NewItemName!);
        File.Move(targetPath, renamedPath);
    }

    private void SaveSymbolItems(string filename)
    {
        // now need to overwrite symbols, will add modified flag to symbol items for optimization later
        foreach (var item in items.Values)
        {
            if (item is SymbolItem symbol)
            {
                string symbolPath = Path.Combine(Path.GetDirectoryName(filename)!, LIBRARY_PATH, symbol.Include.Href);
                symbol.Root?.Save(symbolPath);
            }
        }
    }
}