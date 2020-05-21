﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.NetCore.Analyzers.Performance
{
    /// <summary>
    /// CA1836: Prefer IsEmpty over Count when available.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public abstract class PreferIsEmptyOverCountFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseCountProperlyAnalyzer.CA1836);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode binaryExpression = root.FindNode(context.Span);
            if (binaryExpression == null)
            {
                return;
            }

            ImmutableDictionary<string, string> properties = context.Diagnostics[0].Properties;
            if (properties == null)
            {
                return;
            }

            // Indicates whether the Count property is on the Right or Left side.
            bool useRightSideExpression = properties.ContainsKey(UseCountProperlyAnalyzer.UseRightSideExpressionKey);
            // Indicates if the replacing IsEmpty node should be negated. (!IsEmpty). 
            bool shouldNegate = properties.ContainsKey(UseCountProperlyAnalyzer.ShouldNegateKey);

            context.RegisterCodeFix(CodeAction.Create(
                title: MicrosoftNetCoreAnalyzersResources.PreferIsEmptyOverCountTitle,
                createChangedDocument: async cancellationToken =>
                {
                    DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                    SyntaxGenerator generator = editor.Generator;

                    // The Count property within the binary expression.
                    SyntaxNode countAccessor = GetMemberAccessorFromBinary(binaryExpression, useRightSideExpression);

                    // The object that the Count property belongs to OR null if countAccessor is not a MemberAccessExpressionSyntax.
                    SyntaxNode? objectExpression = GetExpressionFromMemberAccessor(countAccessor);

                    // The IsEmpty property meant to replace the binary expression.
                    SyntaxNode isEmptyNode = objectExpression is null ?
                        generator.IdentifierName(UseCountProperlyAnalyzer.IsEmpty) :
                        generator.MemberAccessExpression(objectExpression, UseCountProperlyAnalyzer.IsEmpty);

                    if (shouldNegate)
                    {
                        isEmptyNode = generator.LogicalNotExpression(isEmptyNode);
                    }

                    editor.ReplaceNode(binaryExpression, isEmptyNode);
                    return editor.GetChangedDocument();
                },
                equivalenceKey: MicrosoftNetCoreAnalyzersResources.PreferIsEmptyOverCountMessage),
            context.Diagnostics);
        }

        // Returns the Expression of node when node is a MemberAccessExpressionSyntax; otherwise, returns null.
        // If this method returns null we assume that node is a IdentifierNameSyntax.
        protected abstract SyntaxNode? GetExpressionFromMemberAccessor(SyntaxNode node);

        protected abstract SyntaxNode GetMemberAccessorFromBinary(SyntaxNode binaryExpression, bool useRightSide);
    }
}
