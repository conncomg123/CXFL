using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaRendering
{
    internal class MainLauncher
    {

        public static void Main(string[] args)
        {
            Document testDoc = new Document("D:\\Documents\\EOJ Assets\\SimpleRenderTest\\SimpleRenderTest.xfl");
            SymbolItem testSymbol = (SymbolItem)testDoc.Library.Items["Horn"];
            Element testElement = testSymbol.Timeline.Layers[0].KeyFrames[0].Elements[0];
            Shape testShape = (Shape)testElement;
            //EdgeUtils.ConvertEdgesToSvgPath(testShape.Edges, testShape.Fills, testShape.Strokes);
            var testing = ShapeUtils.ConvertShapeToSVG(testShape);
        }
    }
}
