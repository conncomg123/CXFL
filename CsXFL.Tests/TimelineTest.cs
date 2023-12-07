public class TimelineTests
{
    // hard-coded constants from the DOMDocument.xml file
    const int INIT_NUM_FRAMES = 3766;
    const int INIT_NUM_LAYERS = 2;
    [Fact]
    public void AddNewLayer_ShouldAddNewLayer_WithCorrectAttributes()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        string name = "Layer1";
        string layerType = "normal";
        int numLayers = timeline.GetLayerCount();

        // Act
        int index = timeline.AddNewLayer(name, layerType);

        // Assert
        Assert.Equal(numLayers + 1, timeline.GetLayerCount());
        Assert.Equal(name, timeline.Layers[index].Name);
        Assert.Equal(layerType, timeline.Layers[index].LayerType);
        Assert.Equal(timeline.GetFrameCount(), timeline.Layers[index].GetFrameCount());
    }
    [Fact]
    public void AddNewLayer_ShouldThrowException_WhenInvalidLayerType()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        string name = "Layer1";
        string layerType = "invalid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => timeline.AddNewLayer(name, layerType));
    }
    [Fact]
    public void GetFrameCount_ShouldReturnCorrectFrameCount()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);

        // Act & Assert
        Assert.Equal(INIT_NUM_FRAMES, timeline.GetFrameCount());
    }
    [Fact]
    public void SetCurrentLayer_ShouldSetCurrentLayer_WhenValidLayerIndex()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = 1;

        // Act
        timeline.SetCurrentLayer(layerIndex);

        // Assert
        for (int i = 0; i < timeline.GetLayerCount(); i++)
        {
            if (i == layerIndex)
            {
                Assert.True(timeline.Layers[i].Current);
            }
            else
            {
                Assert.False(timeline.Layers[i].Current);
            }
        }
    }

    [Fact]
    public void SetCurrentLayer_ShouldNotSetCurrentLayer_WhenInvalidLayerIndex()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.SetCurrentLayer(layerIndex));
        for (int i = 0; i < timeline.GetLayerCount(); i++)
        {
            Assert.False(timeline.Layers[i].Current);
        }
    }
    [Fact]
    public void SetSelectedLayer_ShouldSetSelectedLayer_WhenNotAppendingToCurrentSelection()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = 1;

        // Act
        timeline.SetSelectedLayer(layerIndex, false);

        // Assert
        for (int i = 0; i < timeline.GetLayerCount(); i++)
        {
            if (i == layerIndex)
            {
                Assert.True(timeline.Layers[i].Selected);
            }
            else
            {
                Assert.False(timeline.Layers[i].Selected);
            }
        }
    }

    [Fact]
    public void SetSelectedLayer_ShouldSetSelectedLayer_WhenAppendingToCurrentSelection()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = 1;

        // Act
        timeline.SetSelectedLayer(layerIndex, true);

        // Assert
        Assert.True(timeline.Layers[layerIndex].Selected);
    }
    [Fact]
    public void GetLayerCount_ShouldReturnMatchingLayerCount()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int expectedLayerCount = timeline.Layers.Count;

        // Act
        int actualLayerCount = timeline.GetLayerCount();

        // Assert
        Assert.Equal(expectedLayerCount, actualLayerCount);
    }
    [Fact]
    public void GetLayerCount_ShouldReturnActualLayerCount()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);

        // Act
        int returnedLayerCount = timeline.GetLayerCount();

        // Assert
        Assert.Equal(INIT_NUM_LAYERS, returnedLayerCount);
    }
}
