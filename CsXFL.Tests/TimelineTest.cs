namespace CsXFL.Tests;
public class TimelineTests
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
    // keyframe and has 3766 frames.

    // hard-coded constants from the DOMDocument.xml file
    const int INIT_NUM_FRAMES = 3766;
    const int INIT_NUM_LAYERS = 10;
    const int INIT_FOLDER_IDX = 4;
    const int INIT_NUM_SUBFOLDER_LAYERS = 4;
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
        Assert.Equal(INIT_NUM_FRAMES, timeline.Layers[index].GetFrameCount());
        Assert.Equal(INIT_NUM_LAYERS, index);
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
    [Fact]
    public void FindLayerIndex_ShouldReturnEmptyList_WhenNoMatchingLayerName()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        string name = "NonExistentLayer";

        // Act
        List<int> indices = timeline.FindLayerIndex(name);

        // Assert
        Assert.Empty(indices);
    }

    [Fact]
    public void FindLayerIndex_ShouldReturnMatchingLayerIndices_WhenMatchingLayerName()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        string name = "Layer_1";

        // Act
        List<int> indices = timeline.FindLayerIndex(name);

        // Assert
        Assert.Single(indices);
        Assert.Equal(0, indices[0]);
    }
    [Fact]
    public void DuplicateLayer_ShouldDuplicateSingleLayer_WhenLayerTypeIsNotFolder()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = 1;
        int initialLayerCount = timeline.GetLayerCount();

        // Act
        timeline.DuplicateLayer(layerIndex);

        // Assert
        Assert.Equal(initialLayerCount + 1, timeline.GetLayerCount());
        Assert.Equal(timeline.Layers[layerIndex].Name, timeline.Layers[layerIndex + 1].Name + "_copy");
        Assert.Equal(timeline.Layers[layerIndex].LayerType, timeline.Layers[layerIndex + 1].LayerType);
        Assert.Equal(timeline.Layers[layerIndex].GetFrameCount(), timeline.Layers[layerIndex + 1].GetFrameCount());
    }

    [Fact]
    public void DuplicateLayer_ShouldDuplicateFolderAndChildren_WhenLayerTypeIsFolder()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = INIT_FOLDER_IDX;
        int initialLayerCount = timeline.GetLayerCount();
        int numDuped = INIT_NUM_SUBFOLDER_LAYERS + 1;
        // Act
        timeline.DuplicateLayer(layerIndex);

        // Assert
        Assert.Equal(initialLayerCount + numDuped, timeline.GetLayerCount());
        for (int i = 0; i < numDuped; i++)
        {
            Assert.Equal(timeline.Layers[layerIndex + i].Name, timeline.Layers[layerIndex + i + numDuped].Name + "_copy");
            Assert.Equal(timeline.Layers[layerIndex + i].LayerType, timeline.Layers[layerIndex + i + numDuped].LayerType);
            Assert.Equal(timeline.Layers[layerIndex + i].GetFrameCount(), timeline.Layers[layerIndex + i + numDuped].GetFrameCount());
        }
    }
    [Fact]
    public void DeleteLayer_ShouldRemoveLayer_WhenLayerTypeIsNotFolder()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = 1;
        int initialLayerCount = timeline.GetLayerCount();
        Layer layer = timeline.Layers[layerIndex];

        // Act
        timeline.DeleteLayer(layerIndex);

        // Assert
        Assert.Equal(initialLayerCount - 1, timeline.GetLayerCount());
        Assert.DoesNotContain(layer, timeline.Layers);
    }

    [Fact]
    public void DeleteLayer_ShouldRemoveFolderAndChildren_WhenLayerTypeIsFolder()
    {
        // Arrange
        Document doc = new("TestAssets/DOMDocument.xml");
        Timeline timeline = doc.GetTimeline(0);
        int layerIndex = INIT_FOLDER_IDX;
        int initialLayerCount = timeline.GetLayerCount();
        int numDeleted = INIT_NUM_SUBFOLDER_LAYERS + 1;
        List<Layer> deletedLayers = new();
        for (int i = INIT_FOLDER_IDX; i < INIT_FOLDER_IDX + numDeleted; i++)
        {
            deletedLayers.Add(timeline.Layers[i]);
        }
        // Act
        timeline.DeleteLayer(layerIndex);

        // Assert
        Assert.Equal(initialLayerCount - numDeleted, timeline.GetLayerCount());
        for (int i = 0; i < numDeleted; i++)
        {
            Assert.DoesNotContain(deletedLayers[i], timeline.Layers);
        }
    }
}
