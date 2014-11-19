using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace TSqlAnalyzer.Diagnostics
{
    internal class InterpolatedStringExpressionDiagnostic
    {
        private const string DiagnosticId = "TSqlAnalyzer";
        private const string Title = "Illegal T-SQL";
        private const string MessageFormat = "{0}";
        private const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static void Run(SyntaxNodeAnalysisContext context, InterpolatedStringSyntax token)
        {
            string id = token.ToFullString();
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (id.Contains("\\{") == false)
                return;
            id = id.Replace("\\{", " + {").Replace("}", "} + ");

            string[] list = id.Split('+');

            string sql = BuildSqlStringFromList(list, context, id);

            if (string.IsNullOrWhiteSpace(sql))
                return;

            List<string> errors = SqlParser.Parse(sql);
            if (errors.Count == 0)
                return;

            string errorText = String.Join("\r\n", errors);
            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), errorText);

            context.ReportDiagnostic(diagnostic);
        }

        private static string BuildSqlStringFromList(string[] list, SyntaxNodeAnalysisContext context, string id)
        {
            string sql = string.Empty;
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