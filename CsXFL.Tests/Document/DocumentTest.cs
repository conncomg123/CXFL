using System.Xml.Linq;
using System.IO;
namespace CsXFL.Tests
{
    public class DocumentTests
    {
        [Fact]
        public void Width_ShouldGetAndSet()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            doc.Width = 420;

            // Assert
            Assert.True(doc.Width == 420);
            
        }

        [Fact]
        public void Width_ShouldGetAndSet_WhenNull()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            doc.Root = null;

            // Act
            doc.Width = 420;

            // Assert
            Assert.True(doc.Width == 420);
            
        }

        [Fact]
        public void Height_ShouldGetAndSet()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            doc.Height = 420;

            // Assert
            Assert.True(doc.Height == 420);
            
        }

        [Fact]
        public void Height_ShouldGetAndSet_WhenNull()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            doc.Root = null;

            // Act
            doc.Height = 420;

            // Assert
            Assert.True(doc.Height == 420);
            
        }

        [Fact]
        public void BackgroundColor_ShouldGetAndSet()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            doc.BackgroundColor = "ffffff";

            // Assert
            Assert.True(doc.BackgroundColor == "ffffff");
            
        }

        [Fact]
        public void BackgroundColor_ShouldGetAndSet_WhenNull()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            doc.Root = null;

            // Act
            doc.BackgroundColor = "ffffff";

            // Assert
            Assert.True(doc.BackgroundColor == "ffffff");
            
        }

        [Fact]
        public void FrameRate_ShouldGetAndSet()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);

            // Act
            doc.FrameRate = 23.976;

            // Assert
            Assert.True(doc.FrameRate == 23.976);
            
        }

        [Fact]
        public void FrameRate_ShouldGetAndSet_WhenNull()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            doc.Root = null;

            // Act
            doc.FrameRate = 23.976;

            // Assert
            Assert.True(doc.FrameRate == 23.976);
            
        }

        [Fact]
        public void CurrentTimeline_PlusPlus_ShouldAddOne()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            int curTL = doc.CurrentTimeline;

            // Act
            doc.CurrentTimeline++;

            // Assert
            Assert.True(doc.CurrentTimeline == curTL + 1);
            
        }


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

        // [Fact]
        // public void Save_ShouldThrowException_WhenInvalidFile()
        // {
        //     // Arrange
        //     string filename = "TestAssets/test.fla";
        //     Document doc = new Document(filename);
        //     DateTime lastSaved = File.GetLastWriteTime(filename);

        //     // Act
        //     Action act = () => doc.Save();

        //     // Assert
        //     Assert.Throws<FileNotFoundException>(act);
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



        [Fact]
        public void ImportFile_ShouldImportFileToLibrary()
        {
            // Arrange
            string filename = "TestAssets/DOMDocument.xml";
            Document doc = new Document(filename);
            string assetfilename = "TestAssets/ChadObiWan.png";

            // Act
            doc.ImportFile(assetfilename);

            // Assert
            Assert.True(doc.Library.ItemExists("ChadObiWan.png"));
        }

        [Fact]
        public void ImportFile_ShouldImportFileToDoc()
        {
            // Arrange
            Document doc = new("TestAssets/DOMDocument.xml");
            int itemCount = doc.Library.Items.Count();
            var currentTimeline = doc.GetTimeline(doc.CurrentTimeline);
            var currentLayer = currentTimeline.Layers[currentTimeline.CurrentLayer];
            var currentFrame = currentLayer.GetFrame(currentTimeline.CurrentFrame);
        
            string assetfilename = "TestAssets/ChadObiWan.png";

            // Act
            doc.ImportFile(assetfilename,false);

            // Assert
            Assert.True(doc.Library.ItemExists("ChadObiWan.png"));
            Assert.False(currentFrame.IsEmpty());

        }

        [Fact]
        public void ImportFile_ShouldThrowException_WhenFileNotExist()
        {
            // Arrange
            Document doc = new("TestAssets/DOMDocument.xml");
        
            string assetfilename = "TestAssets/SoyjackAnakin.png";

            // Act
            bool importSuccess = doc.ImportFile(assetfilename);

            // Assert
            Assert.False(importSuccess);
            
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
            string filename = "TestAssets/ChadObiWan.";

            // Act
            Action act = () => new Document(filename);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        
        // [Fact]
        // public void Constructor_ShouldThrowException_IfFileNotExist()
        // {
        //     // Arrange
        //     string filename = "TestAssets/FakeFileName.xfl";

        //     // Act
        //     Action act = () => new Document(filename);

        //     // Assert
        //     Assert.Throws<FileNotFoundException>(act);
        // }

        [Fact]
        public void Constructor_ShouldCreateDoc_TypeIsXFL()
        {
            // Arrange
            string filename = "TestAssets/test.xfl";

            // Act
            Document doc = new Document(filename);

            // Assert
            Assert.NotNull(doc);
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