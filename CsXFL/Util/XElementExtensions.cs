using System.Xml.Linq;
namespace CsXFL;
internal static class XElementExtensions
{
    internal static void SetOrRemoveAttribute<T>(this XElement element, string attributeName, T value, T defaultValue)
    {
        if (EqualityComparer<T>.Default.Equals(value, defaultValue))
        {
            element.Attribute(attributeName)?.Remove();
        }
        else
        {
            element.SetAttributeValue(attributeName, value);
        }
    }

}