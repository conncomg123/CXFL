using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaRendering
{
    internal class MainLauncher
    {
        private static string TESTING_STRING = "!385 862|383 1041!383 1041|614 889";
        private static string TESTING_STRING_TWO = "!385 862|383 1041!383 1041|614 889!614 889|612 2043!612 2043|764 2043!764 2043|766 611!766 611|385 862";
        private static string TESTING_STRING_THREE = "!3532 1539.5S2[#BD9.4D #577.3C 2952.5 1756.5!2952.5 1756.5[#AF6.DA #4C6.1D 3584 1119!3584 1119|3532 1539.5";

        public static void Main(string[] args)
        {
            var testing = EdgeUtils.ConvertEdgeFormatToPointLists(TESTING_STRING_THREE).ToList();
            //EdgeUtils.ConvertPointListToPathFormat(testing.First());
        }
    }
}
