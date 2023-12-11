using System.Xml.Linq;
namespace CsXFL.Tests;
public class LayerTests
{
    // test file has these layers:
    /*
    Layer_1
    Layer_2
    Layer_3
    Layer_4
    folder
        folderlayer
        folderfolder
            folderfolderlayer
        folderlayer_2
    nonfolderlayer
    */
    // ONLY Layer_2 has any content, each non-folder layer has one
    // keyframe and has 3766 frames, except for Layer_1, which has 2 keyframes at the start.
    [Fact]
    public void ClearKeyframe_ShouldRemoveKeyframe_WhenOnTypicalKeyframe()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        Layer layer = timeline.Layers[0];
        Frame removed = layer.Frames[1];
        XElement frameRoot  = removed.Root!;
        int numFrames = layer.GetFrameCount();
        int numKeyframes = layer.Frames.Count;
        // Act
        layer.ClearKeyframe(1);
        // Assert
        Assert.Equal(numFrames, layer.GetFrameCount());
        Assert.Equal(numKeyframes - 1, layer.Frames.Count);
        Assert.DoesNotContain(removed, layer.Frames);
        Assert.True(layer.Root!.Elements().All(e => e.Name != frameRoot.Name || e != frameRoot));
    }
}