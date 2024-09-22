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

    [Fact]
    public void MoveToFolder_ShouldMoveItemToExistingFolder()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        doc.Library.NewFolder("YourMom");
        Item item = doc.Library.Items["Loop.wav"];

        // Act
        doc.Library.MoveToFolder("YourMom", item);

        // Assert
        Assert.True(doc.Library.ItemExists("YourMom/Loop.wav"));


    }

    [Fact]
    public void NewFolder_ShouldCreateDirectoryItem()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");

        // Act
        doc.Library.NewFolder("YourMom");

        // Assert
        Assert.True(doc.Library.ItemExists("YourMom"));

    }
    [Fact]
    public void ItemExists_ReturnsTrue_WhenItemIsInLibrary()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemName = "Loop.wav";

        // Act
        bool exists = doc.Library.ItemExists(itemName);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ItemExists_ReturnsFalse_WhenItemIsNotInLibrary()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemName = "NonExistentItem.wav";

        // Act
        bool exists = doc.Library.ItemExists(itemName);

        // Assert
        Assert.False(exists);
    }
    [Fact]
    public void AddNewItem_AddsItemToLibrary_WhenItemTypeIsValid()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemType = "movie clip";
        string itemName = "TestItem";

        // Act
        Item item = doc.Library.AddNewItem(itemType, itemName);

        // Assert
        Assert.NotNull(item);
        Assert.True(doc.Library.ItemExists(itemName));
    }

    [Fact]
    public void AddNewItem_ThrowsArgumentException_WhenItemAlreadyExists()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemType = "movie clip";
        string itemName = "loop.wav";
        doc.Library.AddNewItem(itemType, itemName);

        // Act and Assert
        Assert.Throws<ArgumentException>(() => doc.Library.AddNewItem(itemType, itemName));
    }

    [Fact]
    public void AddNewItem_ThrowsArgumentException_WhenItemTypeIsInvalid()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemType = "invalid type";
        string itemName = "TestItem";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => doc.Library.AddNewItem(itemType, itemName));
    }

    [Fact]
    public void AddNewItem_ThrowsNotImplementedException_WhenItemTypeIsNotImplemented()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        string itemType = "screen";
        string itemName = "TestItem";

        // Act and Assert
        Assert.Throws<NotImplementedException>(() => doc.Library.AddNewItem(itemType, itemName));
    }
}