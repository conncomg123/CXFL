namespace CsXFL.Tests;
public class TextTest
{

    [Fact]
    public void SetTextString_ShouldReturnWithGetTextString()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        Rectangle rectangle = new Rectangle(1,1,1,1);
        Text text = frame.AddNewText(rectangle, "It's so Joever");

        // Act
        text.SetTextString("Joe Mama");
        

        // Assert
        Assert.True(text.GetTextString() == "Joe Mama");

    }
    

    [Fact]
    public void GetTextString_ShouldReturnTextString()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        Rectangle rectangle = new Rectangle(1,1,1,1);
        Text text = frame.AddNewText(rectangle, "It's so Joever");
        

        // No need to Act, this is just a bare test of get without setting
        // Assert
        Assert.True(text.GetTextString() == "It's so Joever");

    }

    [Fact]
    public void SetTextAttr_ShouldSetATextAttribute()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        Rectangle rectangle = new Rectangle(1,1,1,1);
        Text text = frame.AddNewText(rectangle, "It's so Joever");

        // Act
        text.SetTextAttr("bold", true);

        // Assert
        Assert.True(text.TextRuns[0].TextAttrs.Bold);

    }
}