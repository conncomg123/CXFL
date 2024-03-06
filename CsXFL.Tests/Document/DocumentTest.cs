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
    }
}