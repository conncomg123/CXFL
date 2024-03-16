using System.IO.Compression;
using System.Xml.Linq;

namespace CsXFL;
public class Library
{
    private static int uniqueSoundItemIndex = 0;
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
            SymbolItem symbol = new(symbolTree.Root!, includeNode, this);
            items.Add(symbol.Name, symbol);
        }
        // init SymbolItem timelines
        foreach (Item item in items.Values)
        {
            if (item is SymbolItem symbol)
            {
                _ = symbol.Timeline;
            }
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
            symbolPath = Path.Combine(LIBRARY_PATH, symbolPath).Replace('\\', '/');
            ZipArchiveEntry? symbolEntry = archive.GetEntry(symbolPath);
            if (symbolEntry is null) continue;
            XDocument? symbolTree = XDocument.Load(symbolEntry!.Open());
            if (symbolTree is null) continue;
            SymbolItem symbol = new(symbolTree.Root!, includeNode, this);
            items.Add(symbol.Name, symbol);
        }
        // init SymbolItem timelines
        foreach (Item item in items.Values)
        {
            if (item is SymbolItem symbol)
            {
                _ = symbol.Timeline;
            }
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
    public bool AddItemToDocument(string namePath, Frame? where = null, double posX = 0, double posY = 0)
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
    internal Item? ImportItem(string path, bool isFromOtherDocument = false, string? otherDocumentLibraryRoot = null)
    {
        // todo: create an XElement node from the path, construct an Item from that, then enqueue an Add operation
        if (!File.Exists(path)) return null;
        string itemName;
        if (isFromOtherDocument)
        {
            if (otherDocumentLibraryRoot is not null && path.StartsWith(otherDocumentLibraryRoot))
                itemName = path.Substring(otherDocumentLibraryRoot.Length + 1);
            else itemName = path.Substring(path.IndexOf(LIBRARY_PATH) + LIBRARY_PATH.Length + 1); // best guess
        }
        else
        {
            itemName = Path.GetFileName(path);
        }
        if (itemName.EndsWith(".xml")) itemName = itemName.Substring(0, itemName.Length - 4);
        string targetPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, itemName);
        while (items.ContainsKey(itemName)) // file.exists check is not required
        {
            itemName = Path.Combine(Path.GetDirectoryName(itemName)!, Path.GetFileNameWithoutExtension(itemName) + " copy" + Path.GetExtension(itemName)).Replace('\\', '/');
            targetPath = Path.Combine(Path.GetDirectoryName(containingDocument.Filename)!, LIBRARY_PATH, itemName);
        }
        Item? imported = null;
        if (SYMBOL_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            imported = SymbolItem.FromFile(path, this);
            if (containingDocument.Root!.Element(ns + "symbols") is null) containingDocument.Root!.AddFirst(new XElement(ns + "symbols"));
            containingDocument.Root!.Element(ns + "symbols")!.Add((imported as SymbolItem)!.Include.Root);
            (imported as SymbolItem)!.Timeline.Name = Path.GetFileNameWithoutExtension(itemName);
        }
        else if (AUDIO_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            imported = SoundItem.FromFile(path, ns);
            if (containingDocument.Root!.Element(ns + "media") is null) containingDocument.Root!.AddFirst(new XElement(ns + "media"));
            containingDocument.Root!.Element(ns + "media")!.Add(imported.Root);
        }
        else if (IMAGE_FILE_EXTENSIONS.Contains(Path.GetExtension(path)))
        {
            imported = BitmapItem.FromFile(path, ns);
            if (containingDocument.Root!.Element(ns + "media") is null) containingDocument.Root!.AddFirst(new XElement(ns + "media"));
            containingDocument.Root!.Element(ns + "media")!.Add(imported.Root);
        }
        if (imported is null) return null;
        imported.Name = itemName;
        items.Add(itemName, imported);
        // also add FolderItems for each
        string relativePath = targetPath.Substring(targetPath.IndexOf(LIBRARY_PATH) + LIBRARY_PATH.Length + 1).Replace('\\', '/');
        string[] folders = relativePath.Split('/');
        for (int i = 0; i < folders.Length - 1; i++)
        {
            string folderPath = "";
            for (int j = 0; j <= i; j++)
            {
                folderPath += folders[j] + "/";
            }
            NewFolder(folderPath.TrimEnd('/'));
        }
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
            symbol.Include.Href = newName;
            symbol.Timeline.Name = newName.Substring(newName.LastIndexOf('/') + 1);
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
        LibraryEventMessenger.Instance.NotifyItemRenamed(oldName, newName, item);
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
        LibraryEventMessenger.Instance.NotifyItemRemoved(item);
        items.Remove(itemPath);
        itemOperations.Enqueue(new ItemOperation(item, ItemOperation.OperationType.Remove, itemPath));
        return true;
    }
    private void MoveSingleItemToFolder(string folderName, Item itemToMove)
    {
        string itemName = itemToMove.Name.Substring(itemToMove.Name.LastIndexOf('/') + 1);
        string newItemName = folderName + "/" + itemName;
        RenameItem(itemToMove.Name, newItemName);
    }
    private void MoveFolderItemToFolder(string folderName, FolderItem folderItem)
    {
        string folderPath = folderItem.Name;
        MoveSingleItemToFolder(folderName, folderItem);
        List<Item> itemsToMove = new();
        foreach (Item item in items.Values)
        {
            if (!item.Name.StartsWith(folderPath + "/")) continue;
            itemsToMove.Add(item);
        }
        foreach (Item item in itemsToMove)
        {
            RenameItem(item.Name, folderName + "/" + item.Name);
        }
    }
    public bool MoveToFolder(string folderName, Item itemToMove)
    {
        if (!ItemExists(folderName)) return false;
        if (!items.ContainsKey(itemToMove.Name)) return false;
        if (items[folderName] is not FolderItem) return false;
        if (itemToMove is not FolderItem folder)
        {
            MoveSingleItemToFolder(folderName, itemToMove);
        }
        else
        {
            MoveFolderItemToFolder(folderName, folder);
        }
        return true;
    }
    public bool NewFolder(string folderName)
    {
        if (ItemExists(folderName)) return false;
        FolderItem folder = new(ns)
        {
            Name = folderName
        };
        items.Add(folderName, folder);
        if (containingDocument.Root!.Element(ns + "folders") is null) containingDocument.Root!.AddFirst(new XElement(ns + "folders"));
        containingDocument.Root!.Element(ns + "folders")!.Add(folder.Root);
        return true;
    }
    internal void Save(string filename)
    {
        ProcessItemOperations(filename);
        SaveSymbolItems(filename);
    }

    private void ProcessItemOperations(string filename)
    {
        var addOperations = new List<ItemOperation>();

        while (itemOperations.Count > 0 && itemOperations.Dequeue() is ItemOperation operation)
        {
            string targetPath = Path.Combine(Path.GetDirectoryName(filename)!, LIBRARY_PATH, operation.ItemName);

            if (operation.Type == ItemOperation.OperationType.Add)
            {
                addOperations.Add(operation);
                continue;
            }

            // Process any queued add operations in parallel
            if (addOperations.Count > 0)
            {
                ProcessAddOperationsInParallel(addOperations, filename);
                addOperations.Clear();
            }

            switch (operation.Type)
            {
                case ItemOperation.OperationType.Remove:
                    ProcessRemoveOperation(operation, targetPath, filename);
                    break;
                case ItemOperation.OperationType.Rename:
                    ProcessRenameOperation(operation, targetPath, filename);
                    break;
            }
        }

        // Process any remaining add operations in parallel
        if (addOperations.Count > 0)
        {
            ProcessAddOperationsInParallel(addOperations, filename);
        }
    }

    private void ProcessAddOperationsInParallel(List<ItemOperation> addOperations, string filename)
    {
        int numCores = Environment.ProcessorCount;
        int operationsPerCore = addOperations.Count / numCores;
        var tasks = new List<Task>();

        for (int i = 0; i < numCores; i++)
        {
            int start = i * operationsPerCore;
            int end = (i == numCores - 1) ? addOperations.Count : start + operationsPerCore;

            tasks.Add(Task.Run(() =>
            {
                for (int j = start; j < end; j++)
                {
                    string addTargetPath = Path.Combine(Path.GetDirectoryName(filename)!, LIBRARY_PATH, addOperations[j].ItemName);
                    ProcessAddOperation(addOperations[j], addTargetPath, filename);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
    }

    private void ProcessAddOperation(ItemOperation operation, string targetPath, string filename)
    {
        if (File.Exists(targetPath)) return;
        if (!Directory.Exists(Path.GetDirectoryName(targetPath)!)) Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        File.Copy(operation.NewItemPath!, targetPath);
        // update item's href
        Item item = operation.item;
        if (item is SymbolItem symbol)
        {
            symbol.Include.Href = operation.ItemName;
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

    private static void ProcessSoundItemAdd(SoundItem sound, string targetPath, string filename, string itemName)
    {
        sound.Href = itemName;
        if (sound.Name.EndsWith(".flac"))
        {
            // Animate converts flac files to wav and removes their header, then puts it in the bin folder. Really weird but we gotta do the same thing.
            ArraySegment<byte> wavData = SoundUtils.ConvertFlacToWav(targetPath, sound);
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string datFileName, wavPath;
            do
            {
                int currentIndex = Interlocked.Increment(ref uniqueSoundItemIndex) - 1;
                datFileName = "M " + currentIndex + " " + unixTime + ".dat";
                wavPath = Path.Combine(Path.GetDirectoryName(filename)!, BINARY_PATH, datFileName);
            } while (File.Exists(wavPath));
            using FileStream fs = new(wavPath, FileMode.Create);
            fs.Write(wavData.Array!, WAV_HEADER_SIZE, wavData.Array!.Length - WAV_HEADER_SIZE);
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

    private void ProcessRenameOperation(ItemOperation operation, string targetPath, string filename)
    {
        string renamedPath = Path.Combine(Path.GetDirectoryName(filename)!, LIBRARY_PATH, operation.NewItemName!);
        if (Directory.Exists(targetPath))
            Directory.Move(targetPath, renamedPath);
        else if (File.Exists(targetPath))
            File.Move(targetPath, renamedPath);
        if (operation.item is SymbolItem symbol)
        {
            symbol.Include.Href = operation.NewItemName!;
        }
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