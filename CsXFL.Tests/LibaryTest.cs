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
}