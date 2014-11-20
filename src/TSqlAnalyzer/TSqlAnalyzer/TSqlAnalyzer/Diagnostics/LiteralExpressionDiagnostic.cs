using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TSqlAnalyzer.Diagnostics
{
    internal class LiteralExpressionDiagnostic
    {
        private const string DiagnosticId = "TSqlAnalyzer";
        private const string Title = "Illegal T-SQL";
        private const string MessageFormat = "{0}";
        private const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static void Run(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literalExpression)
        {
            if (literalExpression == null)
                return;

            if (literalExpression.IsKind(SyntaxKind.StringLiteralExpression)
                && literalExpression.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var sql = literalExpression.Token.ValueText;
                if (string.IsNullOrWhiteSpace(sql))
                    return;

                List<string> errors = SqlParser.Parse(sql);
                if (errors.Count == 0)
                    return;

                string errorText = String.Join("\r\n", errors);
                var diagnostic = Diagnostic.Create(Rule, literalExpression.GetLocation(), errorText);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
