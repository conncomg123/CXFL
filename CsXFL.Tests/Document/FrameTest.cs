namespace CsXFL.Tests;
public class FrameTest
{
    [Fact]
    public void ClearElements_ShouldRemoveAllElements()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.ClearElements();

        // Assert
        Assert.True(frame.Elements.Count == 0);

    }

    [Fact]
    public void AddNewText_ShouldCreateTextBox()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        Rectangle rectangle = new Rectangle(1,1,1,1);

        // Act
        frame.AddNewText(rectangle, "It's so Joever");

        // Assert
        Assert.Contains(frame.Elements, element => element is Text text && text.GetTextString() == "It's so Joever");

    }

}