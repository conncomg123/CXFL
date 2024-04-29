using System.Xml.Linq;

namespace CsXFL;
public class FolderItem : Item {
    internal const string FOLDER_NODE_IDENTIFIER = "DOMFolderItem";
    internal FolderItem(XNamespace ns) : base() { 
        this.ns = ns;
        root = new XElement(ns + FolderItem.FOLDER_NODE_IDENTIFIER);
    }
    internal FolderItem(in XElement folderNode) : base(folderNode, "folder") { }
}