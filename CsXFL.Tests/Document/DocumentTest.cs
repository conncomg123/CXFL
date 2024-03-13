using System.Xml.Linq;
namespace CsXFL.Tests
{
    public class DocumentTests
    {
        [Fact]
        public void LoadXFL_ShouldLoadXFLFile()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            XElement? root = doc.Root;

            // Assert
            Assert.NotNull(root);
            Assert.Equal("DOMDocument", root.Name.LocalName);
        }

        // TODO: Create Test for Save
        // [Fact]
        // public void Save_ShouldSaveXFLFile()
        // {
        //     // Arrange
        //     string filename = "TestAssets/DOMDocument.xml";
        //     Document doc = new Document(filename);

        //     // Act

        //     // Assert
        // }

        [Fact]
        public void LoadFLA_ShouldLoadFLAFile()
        {
            // Arrange
            string filename = "TestAssets/test.fla";
            Document doc = new Document(filename);

            // Act
            XElement? root = doc.Root;

            // Assert
            Assert.NotNull(root);
            Assert.Equal("DOMDocument", root.Name.LocalName);
        }

        // TODO: Create Test for Save
        // [Fact]
        // public void Save_ShouldSaveFLAFile()
        // {
        //     // Arrange
        //     string filename = "TestAssets/test.fla";
        //     Document doc = new Document(filename);

        //     // Act

        //     // Assert
        // }

        // TODO: Create Test for Save
        // [Fact]
        // public void Save_ShouldThrowException_WhenInvalidFileExtension()
        // {
        //     // Arrange
        //     string filename = "TestAssets/test.fla";
        //     Document doc = new Document(filename);

        //     // Act

        //     // Assert
        // }

        [Fact]
        public void GetTimeline_ShouldReturnTimeline()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            int timelineIndex = 0;

            // Act
            Timeline timeline = doc.GetTimeline(timelineIndex);

            // Assert
            Assert.NotNull(timeline);
            Assert.Equal(TimelineTests.INIT_NUM_FRAMES, timeline.GetFrameCount());
        }
        [Fact]
        public void Constructor_ShouldThrowException_WhenFileHasWrongExtension()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.txt";

            // Act
            Action act = () => new Document(filename);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        // TODO: Create Test for AddNewScene
        // [Fact]
        // public void AddNewScene_ShouldDoSomething()
        // {
        //     // Arrange
        //     string filename = "TestAssets/DOMDocument.txt";

        //     // Act

        //     // Assert
        // }
        
        // TODO: Create Test for ReorderScene
        // [Fact]
        // public void ReorderScene_ShouldDoSomething()
        // {
        //     // Arrange
        //     string filename = "TestAssets/DOMDocument.txt";

        //     // Act

        //     // Assert
        // }
    }
}