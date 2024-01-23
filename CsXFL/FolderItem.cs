using System.Xml.Linq;

namespace CsXFL;
public class FolderItem : Item {
    internal FolderItem() : base() { }
    internal FolderItem(in XElement folderNode) : base(folderNode, "folder") { }
}