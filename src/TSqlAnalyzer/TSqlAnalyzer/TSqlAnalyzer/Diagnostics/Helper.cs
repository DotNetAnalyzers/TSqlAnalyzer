using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace TSqlAnalyzer.Diagnostics
{
    internal static class Helper
    {
        internal static string BuildSqlStringFromIdString(SyntaxNodeAnalysisContext context, string id)
        {
            string sql = string.Empty;

            if (id.Contains("\\{"))
                id = id.Replace("\\{", " + {").Replace("}", "} + ");

            string[] list = id.Split('+');

            foreach (string s in list)
            {
                if (s.Contains("{") == false)
                {
                    sql += s.Replace("\"", string.Empty);
                }
                else
                {
                    id = s.Replace(" ", "").Replace("{", "").Replace("}", "");

                    BlockSyntax method = context.Node.FirstAncestorOrSelf<BlockSyntax>();
                    if (method == null)
                        break;

                    var t = method.DescendantTokens().Where<SyntaxToken>(st => st.ValueText == id).First<SyntaxToken>();
                    if (string.IsNullOrWhiteSpace(t.ValueText))
                        break;

                    sql += t.GetNextToken().GetNextToken().Value.ToString();
                }
            }
            return sql;
        }
    }
}
