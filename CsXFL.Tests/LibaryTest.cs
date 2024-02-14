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

        // Act
        doc.Library.RemoveItem(itemName);

        // Assert
        Assert.False(doc.Library.ItemExists(itemName));
    }
}