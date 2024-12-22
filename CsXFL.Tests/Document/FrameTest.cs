namespace CsXFL.Tests;
public class FrameTest
{


    [Fact]
    public void XNamespace_ShouldGet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        string cheese = frame.Ns.ToString();

        // Assert
        Assert.True(cheese == "http://ns.adobe.com/xfl/2008/");
    }


    [Fact]
    public void StartFrame_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.StartFrame = 2;

        // Assert
        Assert.True(frame.StartFrame == 2);
    }

    [Fact]
    public void StartFrame_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.StartFrame = 2;

        // Assert
        Assert.True(frame.StartFrame == 2);
    }

    [Fact]
    public void Duration_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.Duration = 7;

        // Assert
        Assert.True(frame.Duration == 7);
    }

    [Fact]
    public void Duration_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.Duration = 7;

        // Assert
        Assert.True(frame.Duration == 7);
    }

    [Fact]
    public void KeyMode_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.KeyMode = 1;

        // Assert
        Assert.True(frame.KeyMode == 1);
    }

    [Fact]
    public void KeyMode_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.KeyMode = 1;

        // Assert
        Assert.True(frame.KeyMode == 1);
    }

    [Fact]
    public void InPoint44_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.InPoint44 = 1;

        // Assert
        Assert.True(frame.InPoint44 == 1);
    }

    [Fact]
    public void InPoint44_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.InPoint44 = 1;

        // Assert
        Assert.True(frame.InPoint44 == 1);
    }

    [Fact]
    public void MotionTweenRotateTimes_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.MotionTweenRotateTimes = 1;

        // Assert
        Assert.True(frame.MotionTweenRotateTimes == 1);
    }

    [Fact]
    public void MotionTweenRotateTimes_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.MotionTweenRotateTimes = 1;

        // Assert
        Assert.True(frame.MotionTweenRotateTimes == 1);
    }

    [Fact]
    public void LabelType_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        

        // Act
        frame.LabelType = "comment";

        // Assert
        Assert.True(frame.LabelType == "comment");
    }

    [Fact]
    public void LabelType_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.LabelType = "comment";

        // Assert
        Assert.True(frame.LabelType == "comment");
    }

    [Fact]
    public void LabelType_ShouldThrowException_WhenNotAcceptableType()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;
        Action setProperty = () => frame.LabelType = "yourmom";

        // Act and Assert
        Assert.Throws<ArgumentException>(setProperty);
    }


    [Fact]
    public void Name_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.Name = "SkibidiRizz";

        // Assert
        Assert.True(frame.Name == "SkibidiRizz");
    }

    [Fact]
    public void Name_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.Name = "SkibidiRizz";

        // Assert
        Assert.True(frame.Name == "SkibidiRizz");
    }

    [Fact]
    public void SoundSync_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.SoundSync = "event";

        // Assert
        Assert.True(frame.SoundSync == "event");
    }

    [Fact]
    public void SoundSync_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.SoundSync = "event";

        // Assert
        Assert.True(frame.SoundSync == "event");
    }

    [Fact]
    public void SoundSync_ShouldThrowException_WhenNotAcceptableType()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;
        Action setProperty = () => frame.SoundSync = "Snoo pingas usual, I see";

        // Act and Assert
        Assert.Throws<ArgumentException>(setProperty);
    }

    [Fact]
    public void TweenType_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.TweenType = "Belieber";

        // Assert
        Assert.True(frame.TweenType == "Belieber");
    }

    [Fact]
    public void TweenType_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.TweenType = "Swifty";

        // Assert
        Assert.True(frame.TweenType == "Swifty");
    }

    [Fact]
    public void MotionTweenSnap_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.MotionTweenSnap = true;

        // Assert
        Assert.True(frame.MotionTweenSnap);
    }

    [Fact]
    public void MotionTweenSnap_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.MotionTweenSnap = true;

        // Assert
        Assert.True(frame.MotionTweenSnap);
    }

    [Fact]
    public void HasCustomEase_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.HasCustomEase = true;

        // Assert
        Assert.True(frame.HasCustomEase);
    }

    [Fact]
    public void HasCustomEase_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.HasCustomEase = true;

        // Assert
        Assert.True(frame.HasCustomEase);
    }

    [Fact]
    public void Bookmark_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.Bookmark = true;

        // Assert
        Assert.True(frame.Bookmark);
    }

    [Fact]
    public void Bookmark_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.Bookmark = true;

        // Assert
        Assert.True(frame.Bookmark);
    }

    [Fact]
    public void UseSingleEaseCurve_ShouldGetAndSet()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
        // Act
        frame.UseSingleEaseCurve = true;

        // Assert
        Assert.True(frame.UseSingleEaseCurve);
    }

    [Fact]
    public void UseSingleEaseCurve_ShouldGetAndSet_WhenNull()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        frame.Root = null;

        // Act
        frame.UseSingleEaseCurve = true;

        // Assert
        Assert.True(frame.UseSingleEaseCurve);
    }

    // [Fact]
    // public void EaseMethodName_ShouldGetAndSet()
    // {
    //     // Arrange
    //     Document doc = new("TestAssets/DOMDocument.xml");
    //     Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
        
    //     // Act
    //     frame.EaseMethodName = "Belieber";

    //     // Assert
    //     Assert.True(frame.EaseMethodName == "Belieber");
    // }

    // [Fact]
    // public void EaseMethodName_ShouldGetAndSet_WhenNull()
    // {
    //     // Arrange
    //     Document doc = new("TestAssets/DOMDocument.xml");
    //     Frame frame = doc.Timelines[0].Layers[0].GetFrame(42);
    //     frame.Root = null;

    //     // Act
    //     frame.EaseMethodName = "Swifty";

    //     // Assert
    //     Assert.True(frame.EaseMethodName == "Swifty");
    // }


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