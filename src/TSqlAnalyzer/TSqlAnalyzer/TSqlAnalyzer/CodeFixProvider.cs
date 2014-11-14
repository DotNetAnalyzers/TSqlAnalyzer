﻿using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace TSqlAnalyzer
{

    //[ExportCodeFixProvider("SqlAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class SqlAnalyzerCodeFixProvider : CodeFixProvider
  {
    public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
    {
        return ImmutableArray.Create(SqlAnalyzerAnalyzer.DiagnosticId);
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task ComputeFixesAsync(CodeFixContext context)
    {
        return;
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

        // Register a code action that will invoke the fix.
        context.RegisterFix(
            CodeAction.Create("Make uppercase", c => MakeUppercaseAsync(context.Document, declaration, c)),
            diagnostic);
    }

    private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
    {
        // Compute new uppercase name.
        var identifierToken = typeDecl.Identifier;
        var newName = identifierToken.Text.ToUpperInvariant();

        // Get the symbol representing the type to be renamed.
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        // Produce a new solution that has all references to that type renamed, including the declaration.
        var originalSolution = document.Project.Solution;
        var optionSet = originalSolution.Workspace.Options;
        var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

        // Return the new solution with the now-uppercase type name.
        return newSolution;
    }
  }
}