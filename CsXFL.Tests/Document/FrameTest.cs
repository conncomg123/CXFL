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

    [Fact]
    public void GetTweenMultiplier_Should()
    {
         // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        Layer layer = timeline.Layers[1];
        int startFrame = 69;

        // Act
        layer.CreateMotionTween(startFrame);
        double value = layer.GetFrame(69).GetTweenMultiplier(startFrame);

        // Assert
        Assert.True(value == 0.018321826872012746);

    }

        [Fact]
        public void RemoveTween_Should()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        Layer layer = timeline.Layers[1];
        int startFrame = 69;


        // Act
        // layer.ConvertToKeyframes(400, 718);
        layer.CreateMotionTween(startFrame);
        layer.GetFrame(startFrame).RemoveTween();
        

        // Assert
        Assert.False(layer.GetFrame(startFrame).TweenType == "motion");

        Assert.False(layer.GetFrame(startFrame).MotionTweenSnap);

        Assert.False(layer.GetFrame(startFrame).EaseMethodName == "none");


    }

}