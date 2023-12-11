namespace CsXFL;
public abstract class Item
{
    private static readonly List<string> AcceptableItemTypes = new List<string> {"undefined",
    "component", "movie clip", "graphic", "button", "folder", "font", "sound", "bitmap", "compiled clip",
    "screen", "video"};
    private string itemType;
}