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
    internal class ExpressionDiagnostic
    {
        private const string DiagnosticId = "TSqlAnalyzer";
        private const string Title = "Illegal T-SQL";
        private const string MessageFormat = "{0}";
        private const string Category = "Naming";

        internal static DiagnosticDescriptor RuleParam = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static void Run(SyntaxNodeAnalysisContext context, ExpressionSyntax token)
        {

            string id = token.ToFullString();
            if (string.IsNullOrWhiteSpace(id))
                return;
            

            if (token.IsKind(SyntaxKind.InvocationExpression))
                return;

            BlockSyntax method = context.Node.FirstAncestorOrSelf<BlockSyntax>();
            if (method == null)
                return;
            try
            {
                var t = method.DescendantTokens().Where<SyntaxToken>(tk => tk.ValueText != null && tk.IsKind(SyntaxKind.IdentifierToken) && tk.ValueText == id).First<SyntaxToken>();

                if (string.IsNullOrWhiteSpace(t.ValueText))
                    return;

                string sql = t.GetNextToken().GetNextToken().Value.ToString();
                if (string.IsNullOrWhiteSpace(sql))
                    return;

                List<string> errors = SqlParser.Parse(sql);
                if (errors.Count == 0)
                    return;

                string errorText = String.Join("\r\n", errors);
                var diagnostic = Diagnostic.Create(RuleParam, t.GetNextToken().GetNextToken().GetLocation(), errorText);

                context.ReportDiagnostic(diagnostic);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("don't handle syntax yet: " + ex.Message);
            }
        }
    }
}