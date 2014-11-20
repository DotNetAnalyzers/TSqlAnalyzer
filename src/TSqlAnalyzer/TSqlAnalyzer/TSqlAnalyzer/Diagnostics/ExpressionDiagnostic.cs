using System;
using System.Collections.Generic;
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

            BlockSyntax method = context.Node.FirstAncestorOrSelf<BlockSyntax>();
            if (method == null)
                return;

            try
            {
                if (token.IsKind(SyntaxKind.InvocationExpression))
                {
                    var nodes = method.DescendantNodes();
                    string s = string.Empty;
                    
                    foreach(SyntaxNode n in nodes)
                    {
                        if(n.IsKind(SyntaxKind.ExpressionStatement) && id.Contains(n.GetFirstToken().Text) && n.ToFullString().Contains("Append("))
                        {
                            string rm = n.GetFirstToken().Text + ".Append(";
                            s += n.GetText().ToString().Replace(rm,"").Replace(@""")","").Replace("\r\n","").Replace(";","") + " ";
                            s = s.Replace("            \"", string.Empty);
                        }
                        
                    }
                    s = Helper.BuildSqlStringFromIdString(context,s);
                    List<string> errorlist = SqlParser.Parse(s);
                    string errorlistText = String.Join("\r\n", errorlist);
                    var diagnostic2 = Diagnostic.Create(RuleParam, context.Node.GetLocation(), errorlistText);

                    context.ReportDiagnostic(diagnostic2);
                    return;
                }
                var t = method.DescendantTokens().Where<SyntaxToken>(tk => tk.ValueText != null && tk.IsKind(SyntaxKind.IdentifierToken) && tk.ValueText == id).First<SyntaxToken>();

                if (string.IsNullOrWhiteSpace(t.ValueText))
                    return;

                string sql = t.GetNextToken().GetNextToken().Value.ToString();
                if (string.IsNullOrWhiteSpace(sql))
                    return;

                List<string> errors = SqlParser.Parse(sql);

                if (errors.Count == 0)
                {
                    var binaryExpressions = method.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().First<BinaryExpressionSyntax>();

                    if (binaryExpressions != null)
                    {
                        BinaryExpressionDiagnostic.Run(context, binaryExpressions);
                        return;
                    }
                    return;
                }
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