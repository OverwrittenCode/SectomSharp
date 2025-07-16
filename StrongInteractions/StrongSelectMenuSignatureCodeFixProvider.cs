using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StrongInteractions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StrongSelectMenuSignatureCodeFixProvider))]
[Shared]
public class StrongSelectMenuSignatureCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add missing string[] parameter";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.InvalidSelectMenuSignature];

    private async Task<Document> AddStringArrayParameterAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
    {
        ParameterSyntax newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("values"))
                                                .WithType(
                                                     SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                                                                  .WithRankSpecifiers(
                                                                       SyntaxFactory.SingletonList(
                                                                           SyntaxFactory.ArrayRankSpecifier(
                                                                               SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())
                                                                           )
                                                                       )
                                                                   )
                                                 );

        SeparatedSyntaxList<ParameterSyntax> oldParams = methodDecl.ParameterList.Parameters;

        bool hasValidLastParam = oldParams.LastOrDefault() is { Type: ArrayTypeSyntax { ElementType: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword } } };

        if (hasValidLastParam)
        {
            return document;
        }

        SeparatedSyntaxList<ParameterSyntax> newParams = oldParams.Add(newParam);
        ParameterListSyntax newParamList = methodDecl.ParameterList.WithParameters(newParams);
        MethodDeclarationSyntax newMethod = methodDecl.WithParameterList(newParamList);

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        SyntaxNode newRoot = root.ReplaceNode(methodDecl, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        MethodDeclarationSyntax? methodDecl = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl is null)
        {
            return;
        }

        context.RegisterCodeFix(CodeAction.Create(Title, c => AddStringArrayParameterAsync(context.Document, methodDecl, c), Title), diagnostic);
    }
}
