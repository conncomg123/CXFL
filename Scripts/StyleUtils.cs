using SkiaSharp;

namespace SkiaRendering
{
    internal class StyleUtils
    {
        public static SKStrokeCap GetStrokeCap(string caps)
        {
            if (caps == "round")
            {
                return SKStrokeCap.Round;
            }
            else if (caps == "butt" || caps == "none")
            {
                return SKStrokeCap.Square;
            }
            else
            {
                return SKStrokeCap.Round;
            }
        }

        public static SKStrokeJoin GetStrokeJoin(string joints)
        {
            if (joints == "round")
            {
                return SKStrokeJoin.Round;
            }
            else if (joints == "butt")
            {
                return SKStrokeJoin.Bevel;
            }
            else
            {
                return SKStrokeJoin.Miter;
            }
        }
    }
}
