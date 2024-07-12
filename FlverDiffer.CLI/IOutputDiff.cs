using System.Text;
using System.Web;

namespace FlverDiffer.CLI;

public interface IOutputDiff
{
    public string Output(IEnumerable<ValueDifference> differences);
}

public class HTMLOutput(string template) : IOutputDiff
{
    public string Output(IEnumerable<ValueDifference> differences)
    {
        StringBuilder sb = new();
     
        foreach (var difference in differences)
        {
            sb.AppendLine(ToTableRow(difference));
        }

        StringBuilder final = new StringBuilder(template);
        

        return final.Replace("@TableBody", sb.ToString()).ToString();
    }

    private static string ToTableRow(ValueDifference difference)
    {
        return @$"
            <tr>
                <td>{HttpUtility.HtmlEncode(difference.FieldPath)}</td>
                <td class='d-flex flex-column'>
                    <span>{HttpUtility.HtmlEncode(difference.Value1)}</span>
                    <span>{HttpUtility.HtmlEncode(difference.Value2)}</span>
                </td>
            </tr>        
        ";
    }
}
