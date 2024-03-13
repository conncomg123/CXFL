using System.Xml.Linq;
using System.IO;
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
        public void Save_ShouldSaveXFLFile()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            DateTime lastSaved = File.GetLastWriteTime(filename);

            // Act
            doc.Save();
            DateTime newSaved = File.GetLastWriteTime(filename);

            // Assert
            Assert.True(newSaved > lastSaved);
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
        public void Save_ShouldSaveFLAFile()
        {
            // Arrange
            string filename = "TestAssets/test.fla";
            Document doc = new Document(filename);
            DateTime lastSaved = File.GetLastWriteTime(filename);

            // Act
            doc.Save();
            DateTime newSaved = File.GetLastWriteTime(filename);

            // Assert
            Assert.True(newSaved > lastSaved);
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

        [Fact]
        public void AddNewScene_ShouldAddSceneToTimelinesArray()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            doc.AddNewScene("Your mom");

            // Assert
            Assert.Contains(doc.Timelines, timeline => timeline.Name == "Your mom");
            Assert.DoesNotContain(doc.Timelines, timeline => timeline.Name == "Your face");
        }


        [Fact]
        public void ReorderScene_ShouldAddBefore()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            Timeline scene2 = doc.Timelines[2];

            // Act
            doc.ReorderScene(2, 0);

            // Assert
            Assert.True(doc.Timelines[0] == scene2);

        }

        [Fact]
        public void ReorderScene_ShouldAddAfter()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            Timeline scene2 = doc.Timelines[2];

            // Act
            doc.ReorderScene(2, 0, false);

            // Assert
            Assert.True(doc.Timelines[1] == scene2);

        }
    }
}