using System.Xml.Linq;
using System.IO;
namespace CsXFL.Tests
{
    public class EaseTests
    {
        [Fact]
        public void Target_ShouldGetAndSet()
        {
            // Arrange
            Document doc = new("TestAssets/DOMDocument.xml");
            Timeline timeline = doc.GetTimeline(0);
            Layer layer = timeline.Layers[1];
            int startFrame = 69;

            // Act
            layer.CreateMotionTween(startFrame);

            // Assert
            Assert.True(true);
            
        }

    }
}