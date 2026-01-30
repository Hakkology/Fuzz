using System.Text.RegularExpressions;
using System.Web;

namespace Fuzz.Web.Utilities;

public static class MarkdownHelper
{
    public static string ToHtml(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";
        
        // Basic HTML encoding for security
        content = HttpUtility.HtmlEncode(content);
        
        // Bold: **text** -> <strong>text</strong>
        content = Regex.Replace(content, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        
        // Newlines to <br/>
        content = content.Replace("\n", "<br/>");
        
        return content;
    }
}
