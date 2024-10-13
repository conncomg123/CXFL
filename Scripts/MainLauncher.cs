using CsXFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rendering
{
    internal class MainLauncher
    {

        public static void Run(string[] args)
        {
            // Create a dummy group
            var group = new XElement("g");

            // Create a CompoundFilter
            var compoundFilter = new FilterUtils.CompoundFilter
            {
                Name = "MyFilter",
                Filters = new List<FilterUtils.AtomicFilter>
                {
                    new FilterUtils.FeFlood("red", "0.5"),
                    new FilterUtils.FeOffset(10, 20),
                    new FilterUtils.FeGaussianBlur(5, 25),
                    new FilterUtils.FeBlend("SourceGraphic", "overlay"),
                    new FilterUtils.FeColorMatrix("0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0", "matrix"),
                    new FilterUtils.FeComposite("xor", "SourceGraphic"),
                    new FilterUtils.FeTurbulence("turbulence", 0.5, 5, 0, true),
                    new FilterUtils.FeDisplacementMap("turbulence", "SourceGraphic", 0.5, "R", "G"),
                    new FilterUtils.FeDropShadow(10, 20, 5, "red", 0.5)
                }
            };

            // Apply the filter to the group
            var filterUtils = new FilterUtils();
            var (filterDefs, filteredGroup) = filterUtils.ApplyFilter(group, compoundFilter);

            // Print the resulting XML
            Console.WriteLine(filterDefs.ToString());
            Console.WriteLine(filteredGroup.ToString());
        }
    }
}
