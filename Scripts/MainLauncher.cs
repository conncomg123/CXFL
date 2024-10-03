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

        public static void Run(string[] args)
        {
            Document testDoc = new Document("C:\\Users\\Administrator\\Desktop\\TextRendering\\TextRendering.fla");
            var svgSave = new SVGRenderer(testDoc).Render(testDoc.Timelines[0], 0, testDoc.Width, testDoc.Height);
            svgSave.Save("C:\\Users\\Administrator\\Desktop\\TextRendering\\TextRendering_export.svg");
        }
    }
}
