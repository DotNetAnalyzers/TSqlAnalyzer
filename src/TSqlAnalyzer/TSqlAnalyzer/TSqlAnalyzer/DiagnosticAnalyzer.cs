using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace TSqlAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SqlAnalyzerAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "TSqlAnalyzer";
		internal const string Title = "Illegal T-SQL";
		internal const string MessageFormat = "{0}";
		internal const string Category = "Naming";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static DiagnosticDescriptor RuleParam = new DiagnosticDescriptor(DiagnosticId+"1", Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule).Add(RuleParam); } }

		public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			context.RegisterSyntaxNodeAction(AnalyzeConstructorNode, SyntaxKind.ObjectCreationExpression);
			context.RegisterSyntaxNodeAction(AnalyzeAssignmentNode, SyntaxKind.SimpleAssignmentExpression);
		}

		private static void AnalyzeAssignmentNode(SyntaxNodeAnalysisContext context)
		{
			var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

			if (!assignmentExpression.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				return;

			//TODO Is it possible to detect type info?
			if (!assignmentExpression.Left.ToString().Contains("CommandText"))
				return;

			var literalExpression = assignmentExpression.Right as LiteralExpressionSyntax;

			RunDiagnostics(context, literalExpression);
		}


		private static void AnalyzeConstructorNode(SyntaxNodeAnalysisContext context)
		{
			var objectCreationExpression = (ObjectCreationExpressionSyntax)context.Node;

			//TODO Is it possible to detect type info?
			if (!objectCreationExpression.Type.ToString().Contains("SqlCommand"))
				return;

			if (objectCreationExpression.ArgumentList.Arguments.Count == 0)
				return;

			ExpressionSyntax expressionSyntax = objectCreationExpression.ArgumentList.Arguments.First().Expression;

			var literalExpression = expressionSyntax as LiteralExpressionSyntax;
            if (literalExpression != null)
            {
                RunDiagnostics(context, literalExpression);
                return;
            }
            RunDiagnostics(context, expressionSyntax);
        }

        private static void RunDiagnostics(SyntaxNodeAnalysisContext context, ExpressionSyntax token)
        {
          
            string id = token.ToFullString();
            if (string.IsNullOrWhiteSpace(id))
                return;

            BlockSyntax method = context.Node.FirstAncestorOrSelf<BlockSyntax>();
            if (method == null)
                return;

            var t = method.DescendantTokens().Where<SyntaxToken>(tk => tk.IsKind(SyntaxKind.IdentifierToken) && tk.ValueText == id).First<SyntaxToken>();

            string sql = t.GetNextToken().GetNextToken().Value.ToString();
            if(string.IsNullOrWhiteSpace(sql))
                return;

            List<string> errors = SqlParser.Parse(sql);
            if (errors.Count == 0)
                return;

            string errorText = String.Join("\r\n", errors);
            var diagnostic = Diagnostic.Create(RuleParam, t.GetNextToken().GetNextToken().GetLocation(), errorText);

            context.ReportDiagnostic(diagnostic);

            /*if (token.Parent.IsKind(SyntaxKind.IdentifierName) == false)
                return;
            
            if (token.Parent.Parent.Parent.IsKind(SyntaxKind.StringLiteralExpression) == false)
                return;
                */
            // var literalExpression = token.Parent.Parent as LiteralExpressionSyntax;


            //var variableDeclarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("sql")).WithInitializer(SyntaxFactory.EqualsValueClause(literalExpression));
            //if (literalExpression != null)
            //  RunDiagnostics(context, literalExpression);

        }

        private static void RunDiagnostics(SyntaxNodeAnalysisContext context, LiteralExpressionSyntax literalExpression)
		{
			if (literalExpression == null)
			    return;
			
			if (literalExpression.IsKind(SyntaxKind.StringLiteralExpression)
				&& literalExpression.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				var sql = literalExpression.Token.ValueText;
				if (string.IsNullOrWhiteSpace(sql) )
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
