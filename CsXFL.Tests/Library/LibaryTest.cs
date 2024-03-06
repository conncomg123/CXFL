namespace CsXFL.Tests;
public class LibraryTest
{
    [Fact]
    public void RenameItem_ShouldRenameItem_WhenItemExists()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemName = "Loop.wav";
        string newName = "Loop2.wav";

        // Act
        doc.Library.RenameItem(itemName, newName);

        // Assert
        Assert.True(doc.Library.ItemExists(newName));
        Assert.False(doc.Library.ItemExists(itemName));
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItem()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemName = "Loop.wav";
        int itemCount = doc.Library.Items.Count();

        // Act
        doc.Library.RemoveItem(itemName);

        // Assert
        Assert.False(doc.Library.ItemExists(itemName));
        Assert.True(doc.Library.Items.Count() == itemCount - 1);

    }

    [Fact]
    public void ImportItem_ShouldAddItem()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        int itemCount = doc.Library.Items.Count();

        // Act
        doc.Library.ImportItem("TestAssets/WilhelmScream.wav");
        doc.Library.ImportItem("TestAssets/ChadObiWan.png");
        doc.Library.ImportItem("TestAssets/tmp_Dummysymbol.xml");


        // Assert
        Assert.True(doc.Library.ItemExists("WilhelmScream.wav"));
        Assert.True(doc.Library.ItemExists("ChadObiWan.png"));
        Assert.True(doc.Library.ItemExists("tmp_Dummysymbol"));
        Assert.True(doc.Library.Items.Count() == itemCount + 3);

    }

    [Fact]
    public void Save_ShouldSaveLibraryToDisk()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        int itemCount = doc.Library.Items.Count();

        // Act
        doc.Library.ImportItem("TestAssets/ChadObiWan.png");
        doc.Library.Save("TestAssets/DOMDocument.xml");

        // Assert
        Assert.True(doc.Library.ItemExists("ChadObiWan.png"));
        Assert.True(doc.Library.Items.Count() == itemCount + 1);

        // Cleanup
        doc.Library.RemoveItem("ChadObiWan.png");
        doc.Library.Save("TestAssets/DOMDocument.xml");
        Assert.False(doc.Library.ItemExists("ChadObiWan.png"));
        Assert.True(doc.Library.Items.Count() == itemCount);
    }

    [Fact]
    public void AddItemToDocument_ShouldInsertItem()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        int itemCount = doc.Library.Items.Count();
        var currentTimeline = doc.GetTimeline(doc.CurrentTimeline);
        var currentLayer = currentTimeline.Layers[currentTimeline.CurrentLayer];
        var currentFrame = currentLayer.GetFrame(currentTimeline.CurrentFrame);

        // Act
        doc.Library.AddItemToDocument("Alert.wav");
        doc.Library.AddItemToDocument("Bracelet.png", doc.Timelines[0].Layers[0].GetFrame(42), 69, 420);
        doc.Library.AddItemToDocument("Symbol 1");


        // Assert
        Assert.False(currentFrame.IsEmpty());
        Assert.True(currentFrame.SoundName == "Alert.wav");
        Assert.True(currentFrame.Elements[0] is SymbolInstance);
        Assert.Contains(currentFrame.Elements, element => element is SymbolInstance { LibraryItemName: "Symbol 1" });
        Assert.False(doc.Timelines[0].Layers[0].GetFrame(42).IsEmpty());

    }

}