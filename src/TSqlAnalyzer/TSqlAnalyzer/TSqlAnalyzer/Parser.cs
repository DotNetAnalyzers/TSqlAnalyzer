using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;

namespace TSqlAnalyzer
{
  public class SqlParser
  {
    public static List<string> Parse(string sql)
    {
      TSql120Parser parser = new TSql120Parser(false);

      IList<ParseError> errors;
      parser.Parse(new StringReader(sql), out errors);
      if (errors != null && errors.Count > 0)
      {
        List<string> errorList = new List<string>();
        foreach (var error in errors)
        {
          errorList.Add(error.Message);
        }
        return errorList;
      }

      return new List<string>(); ;
    }
  }
}
