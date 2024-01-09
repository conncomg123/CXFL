using System.Xml.Linq;

namespace CsXFL;
public class FolderItem : Item {
    public FolderItem() : base() { }
    public FolderItem(in XElement folderNode) : base(folderNode, "folder") { }
}