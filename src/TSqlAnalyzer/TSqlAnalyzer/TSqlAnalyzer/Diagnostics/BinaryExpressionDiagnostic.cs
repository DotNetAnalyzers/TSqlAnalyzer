using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace TSqlAnalyzer.Diagnostics
{
    internal class BinaryExpressionDiagnostic
    {
        private const string DiagnosticId = "TSqlAnalyzer";
        private const string Title = "Illegal T-SQL";
        private const string MessageFormat = "{0}";
        private const string Category = "Naming";

        internal static DiagnosticDescriptor RuleParam1 = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static void Run(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax token)
        {
            string id = token.ToFullString();
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (id.Contains("+") == false)
                return;

            string[] list = id.Split('+');
            if (list.Count() != 2)
                return;

            string sql = list[0];
            sql = sql.Replace("\"", string.Empty);

            if (list[1].Contains("\""))
            {
                sql += list[1].Replace("\"", string.Empty);
            }
            else
            {
                id = list[1].Replace(" ", "");

                BlockSyntax method = context.Node.FirstAncestorOrSelf<BlockSyntax>();
                if (method == null)
                    return;
            
                var t = method.DescendantTokens().Where<SyntaxToken>(st => st.ValueText == id).First<SyntaxToken>();
                if (string.IsNullOrWhiteSpace(t.ValueText))
                    return;

                sql += t.GetNextToken().GetNextToken().Value.ToString();
            }

            if (string.IsNullOrWhiteSpace(sql))
                return;

            List<string> errors = SqlParser.Parse(sql);
            if (errors.Count == 0)
                return;

            string errorText = String.Join("\r\n", errors);
            var diagnostic = Diagnostic.Create(RuleParam1, context.Node.GetLocation(), errorText);

            context.ReportDiagnostic(diagnostic);
        }
    }
}