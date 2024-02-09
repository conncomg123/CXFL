using System.Xml.Linq;

namespace CsXFL;
public class FolderItem : Item {
    internal FolderItem(XNamespace ns) : base() { 
        this.ns = ns;
        root = new XElement(ns + "DOMFolderItem");
    }
    internal FolderItem(in XElement folderNode) : base(folderNode, "folder") { }
}