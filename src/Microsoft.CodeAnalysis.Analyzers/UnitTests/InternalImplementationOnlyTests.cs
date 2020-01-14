﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.InternalImplementationOnlyAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.InternalImplementationOnlyAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class InternalImplementationOnlyTests
    {
        private const string AttributeStringCSharp = @"
namespace System.Runtime.CompilerServices
{
    internal class InternalImplementationOnlyAttribute : System.Attribute {}
}
";
        [Fact]
        public async Task CSharp_VerifySameAssembly()
        {
            string source = AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

class Foo : IFoo { }
";

            // Verify no diagnostic since interface is in the same assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyDifferentAssembly()
        {
            string source1 = AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

public interface IBar : IFoo { }
";

            var source2 = @"
class Foo : IFoo { }

class Boo : IBar { }";

            // Verify errors since interface is not in a friend assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(2,7): error RS1009: Type Foo cannot implement interface IFoo because IFoo is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(2, 7, 2, 10).WithArguments("Foo", "IFoo"),
                        // Test0.cs(4,7): error RS1009: Type Boo cannot implement interface IFoo because IFoo is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(4, 7, 4, 10).WithArguments("Boo", "IFoo"),
                    },
                },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var dependencyProject = solution.AddProject("DependencyProject", "DependencyProject", LanguageNames.CSharp)
                            .WithCompilationOptions(solution.GetProject(projectId).CompilationOptions)
                            .WithParseOptions(solution.GetProject(projectId).ParseOptions)
                            .WithMetadataReferences(solution.GetProject(projectId).MetadataReferences)
                            .AddDocument("Test0.cs", source1, filePath: "Test0.cs").Project;

                        return dependencyProject.Solution.AddProjectReference(projectId, new ProjectReference(dependencyProject.Id));
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyDifferentFriendAssembly()
        {
            string source1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""TestProject"")]
" + AttributeStringCSharp + @"

[System.Runtime.CompilerServices.InternalImplementationOnly]
public interface IFoo { }

public interface IBar : IFoo { }
";

            var source2 = @"
class Foo : IFoo { }

class Boo : IBar { }";

            // Verify no diagnostic since interface is in a friend assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source2 } },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var dependencyProject = solution.AddProject("DependencyProject", "DependencyProject", LanguageNames.CSharp)
                            .WithCompilationOptions(solution.GetProject(projectId).CompilationOptions)
                            .WithParseOptions(solution.GetProject(projectId).ParseOptions)
                            .WithMetadataReferences(solution.GetProject(projectId).MetadataReferences)
                            .AddDocument("Test0.cs", source1, filePath: "Test0.cs").Project;

                        return dependencyProject.Solution.AddProjectReference(projectId, new ProjectReference(dependencyProject.Id));
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyISymbol()
        {
            var source = @"
// Causes many compile errors, because not all members are implemented.
class Foo : Microsoft.CodeAnalysis.ISymbol { }
class Bar : Microsoft.CodeAnalysis.IAssemblySymbol { }
";

            // Verify that ISymbol is not implementable.
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(3,7): error RS1009: Type Foo cannot implement interface ISymbol because ISymbol is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(3, 7, 3, 10).WithArguments("Foo", "ISymbol"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Accept(SymbolVisitor)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Accept(Microsoft.CodeAnalysis.SymbolVisitor)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Accept<TResult>(SymbolVisitor<TResult>)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Accept<TResult>(Microsoft.CodeAnalysis.SymbolVisitor<TResult>)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.CanBeReferencedByName'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.CanBeReferencedByName"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ContainingAssembly'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ContainingAssembly"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ContainingModule'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ContainingModule"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ContainingNamespace'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ContainingNamespace"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ContainingSymbol'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ContainingSymbol"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ContainingType'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ContainingType"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.DeclaredAccessibility'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.DeclaredAccessibility"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.DeclaringSyntaxReferences'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.DeclaringSyntaxReferences"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.GetAttributes()'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.GetAttributes()"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.GetDocumentationCommentId()'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.GetDocumentationCommentId()"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.GetDocumentationCommentXml(CultureInfo, bool, CancellationToken)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.GetDocumentationCommentXml(System.Globalization.CultureInfo, bool, System.Threading.CancellationToken)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.HasUnsupportedMetadata'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.HasUnsupportedMetadata"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsAbstract'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsAbstract"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsDefinition'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsDefinition"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsExtern'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsExtern"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsImplicitlyDeclared'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsImplicitlyDeclared"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsOverride'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsOverride"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsSealed'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsSealed"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsStatic'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsStatic"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.IsVirtual'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.IsVirtual"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Kind'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Kind"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Language'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Language"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Locations'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Locations"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.MetadataName'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.MetadataName"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.Name'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.Name"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.OriginalDefinition'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.OriginalDefinition"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ToDisplayParts(SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ToDisplayParts(Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ToDisplayString(SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ToMinimalDisplayParts(SemanticModel, int, SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ToMinimalDisplayParts(Microsoft.CodeAnalysis.SemanticModel, int, Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'ISymbol.ToMinimalDisplayString(SemanticModel, int, SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "Microsoft.CodeAnalysis.ISymbol.ToMinimalDisplayString(Microsoft.CodeAnalysis.SemanticModel, int, Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IEquatable<ISymbol>.Equals(ISymbol)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 43).WithArguments("Foo", "System.IEquatable<Microsoft.CodeAnalysis.ISymbol>.Equals(Microsoft.CodeAnalysis.ISymbol)"),
                        // Test0.cs(4,7): error RS1009: Type Bar cannot implement interface ISymbol because ISymbol is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(4, 7, 4, 10).WithArguments("Bar", "ISymbol"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.GetMetadata()'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.GetMetadata()"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.GetTypeByMetadataName(string)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.GetTypeByMetadataName(string)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.GivesAccessTo(IAssemblySymbol)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.GivesAccessTo(Microsoft.CodeAnalysis.IAssemblySymbol)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.GlobalNamespace'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.GlobalNamespace"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.Identity'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.Identity"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.IsInteractive'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.IsInteractive"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.MightContainExtensionMethods'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.MightContainExtensionMethods"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.Modules'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.Modules"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.NamespaceNames'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.NamespaceNames"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.ResolveForwardedType(string)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.ResolveForwardedType(string)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IAssemblySymbol.TypeNames'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.IAssemblySymbol.TypeNames"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Accept(SymbolVisitor)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Accept(Microsoft.CodeAnalysis.SymbolVisitor)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Accept<TResult>(SymbolVisitor<TResult>)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Accept<TResult>(Microsoft.CodeAnalysis.SymbolVisitor<TResult>)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.CanBeReferencedByName'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.CanBeReferencedByName"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ContainingAssembly'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ContainingAssembly"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ContainingModule'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ContainingModule"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ContainingNamespace'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ContainingNamespace"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ContainingSymbol'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ContainingSymbol"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ContainingType'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ContainingType"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.DeclaredAccessibility'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.DeclaredAccessibility"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.DeclaringSyntaxReferences'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.DeclaringSyntaxReferences"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.GetAttributes()'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.GetAttributes()"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.GetDocumentationCommentId()'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.GetDocumentationCommentId()"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.GetDocumentationCommentXml(CultureInfo, bool, CancellationToken)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.GetDocumentationCommentXml(System.Globalization.CultureInfo, bool, System.Threading.CancellationToken)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.HasUnsupportedMetadata'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.HasUnsupportedMetadata"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsAbstract'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsAbstract"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsDefinition'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsDefinition"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsExtern'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsExtern"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsImplicitlyDeclared'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsImplicitlyDeclared"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsOverride'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsOverride"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsSealed'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsSealed"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsStatic'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsStatic"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.IsVirtual'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.IsVirtual"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Kind'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Kind"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Language'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Language"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Locations'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Locations"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.MetadataName'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.MetadataName"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.Name'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.Name"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.OriginalDefinition'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.OriginalDefinition"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ToDisplayParts(SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ToDisplayParts(Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ToDisplayString(SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ToMinimalDisplayParts(SemanticModel, int, SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ToMinimalDisplayParts(Microsoft.CodeAnalysis.SemanticModel, int, Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'ISymbol.ToMinimalDisplayString(SemanticModel, int, SymbolDisplayFormat)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "Microsoft.CodeAnalysis.ISymbol.ToMinimalDisplayString(Microsoft.CodeAnalysis.SemanticModel, int, Microsoft.CodeAnalysis.SymbolDisplayFormat)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IEquatable<ISymbol>.Equals(ISymbol)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 51).WithArguments("Bar", "System.IEquatable<Microsoft.CodeAnalysis.ISymbol>.Equals(Microsoft.CodeAnalysis.ISymbol)")
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyIOperation()
        {
            var source = @"
// Causes many compile errors, because not all members are implemented.
class Foo : Microsoft.CodeAnalysis.IOperation { }
class Bar : Microsoft.CodeAnalysis.Operations.IInvocationOperation { }
";

            // Verify that IOperation is not implementable.
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(3,7): error RS1009: Type Foo cannot implement interface IOperation because IOperation is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(3, 7, 3, 10).WithArguments("Foo", "IOperation"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Accept(OperationVisitor)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Accept(Microsoft.CodeAnalysis.Operations.OperationVisitor)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult>, TArgument)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Accept<TArgument, TResult>(Microsoft.CodeAnalysis.Operations.OperationVisitor<TArgument, TResult>, TArgument)"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Children'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Children"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.ConstantValue'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.ConstantValue"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.IsImplicit'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.IsImplicit"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Kind'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Kind"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Language'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Language"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Parent'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Parent"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.SemanticModel'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.SemanticModel"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Syntax'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Syntax"),
                        // Test0.cs(3,13): error CS0535: 'Foo' does not implement interface member 'IOperation.Type'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(3, 13, 3, 46).WithArguments("Foo", "Microsoft.CodeAnalysis.IOperation.Type"),
                        // Test0.cs(4,7): error RS1009: Type Bar cannot implement interface IOperation because IOperation is not available for public implementation.
                        VerifyCS.Diagnostic().WithSpan(4, 7, 4, 10).WithArguments("Bar", "IOperation"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Accept(OperationVisitor)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Accept(Microsoft.CodeAnalysis.Operations.OperationVisitor)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult>, TArgument)'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Accept<TArgument, TResult>(Microsoft.CodeAnalysis.Operations.OperationVisitor<TArgument, TResult>, TArgument)"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Children'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Children"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.ConstantValue'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.ConstantValue"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.IsImplicit'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.IsImplicit"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Kind'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Kind"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Language'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Language"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Parent'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Parent"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.SemanticModel'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.SemanticModel"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Syntax'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Syntax"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IOperation.Type'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.IOperation.Type"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IInvocationOperation.Arguments'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.Operations.IInvocationOperation.Arguments"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IInvocationOperation.Instance'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.Operations.IInvocationOperation.Instance"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IInvocationOperation.IsVirtual'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.Operations.IInvocationOperation.IsVirtual"),
                        // Test0.cs(4,13): error CS0535: 'Bar' does not implement interface member 'IInvocationOperation.TargetMethod'
                        DiagnosticResult.CompilerError("CS0535").WithSpan(4, 13, 4, 67).WithArguments("Bar", "Microsoft.CodeAnalysis.Operations.IInvocationOperation.TargetMethod")
                    },
                },
            }.RunAsync();
        }

        private const string AttributeStringBasic = @"
Namespace System.Runtime.CompilerServices
    Friend Class InternalImplementationOnlyAttribute 
        Inherits System.Attribute
    End Class
End Namespace
";

        [Fact]
        public async Task Basic_VerifySameAssembly()
        {
            string source = AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Class Foo 
    Implements IFoo 
End Class
";

            // Verify no diagnostic since interface is in the same assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyDifferentAssembly()
        {
            string source1 = AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Public Interface IBar
    Inherits IFoo
End Interface
";

            var source2 = @"
Class Foo 
    Implements IFoo 
End Class

Class Bar
    Implements IBar
End Class
";

            // Verify errors since interface is not in a friend assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(2,7): error RS1009: Type Foo cannot implement interface IFoo because IFoo is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(2, 7, 2, 10).WithArguments("Foo", "IFoo"),
                        // Test0.vb(6,7): error RS1009: Type Bar cannot implement interface IFoo because IFoo is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(6, 7, 6, 10).WithArguments("Bar", "IFoo"),
                    },
                },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var dependencyProject = solution.AddProject("DependencyProject", "DependencyProject", LanguageNames.VisualBasic)
                            .WithCompilationOptions(solution.GetProject(projectId).CompilationOptions)
                            .WithParseOptions(solution.GetProject(projectId).ParseOptions)
                            .WithMetadataReferences(solution.GetProject(projectId).MetadataReferences)
                            .AddDocument("Test0.vb", source1, filePath: "Test0.vb").Project;

                        return dependencyProject.Solution.AddProjectReference(projectId, new ProjectReference(dependencyProject.Id));
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyDifferentFriendAssembly()
        {
            string source1 = @"
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""TestProject"")>
" + AttributeStringBasic + @"

<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IFoo
End Interface

Public Interface IBar
    Inherits IFoo
End Interface
";

            var source2 = @"
Class Foo 
    Implements IFoo 
End Class

Class Bar
    Implements IBar
End Class
";

            // Verify no diagnostic since interface is in a friend assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source2 } },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var dependencyProject = solution.AddProject("DependencyProject", "DependencyProject", LanguageNames.VisualBasic)
                            .WithCompilationOptions(solution.GetProject(projectId).CompilationOptions)
                            .WithParseOptions(solution.GetProject(projectId).ParseOptions)
                            .WithMetadataReferences(solution.GetProject(projectId).MetadataReferences)
                            .AddDocument("Test0.vb", source1, filePath: "Test0.vb").Project;

                        return dependencyProject.Solution.AddProjectReference(projectId, new ProjectReference(dependencyProject.Id));
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyISymbol()
        {
            var source = @"
' Causes many compile errors, because not all members are implemented.
Class Foo 
    Implements Microsoft.CodeAnalysis.ISymbol
End Class
Class Bar
    Implements Microsoft.CodeAnalysis.IAssemblySymbol
End Class
";

            // Verify that ISymbol is not implementable.
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(3,7): error RS1009: Type Foo cannot implement interface ISymbol because ISymbol is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(3, 7, 3, 10).WithArguments("Foo", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function Accept(Of TResult)(visitor As Microsoft.CodeAnalysis.SymbolVisitor(Of TResult)) As TResult", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function Equals(other As ISymbol) As Boolean' for interface 'IEquatable(Of ISymbol)'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function Equals(other As Microsoft.CodeAnalysis.ISymbol) As Boolean", "IEquatable(Of ISymbol)"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function GetAttributes() As ImmutableArray(Of AttributeData)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function GetAttributes() As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.AttributeData)", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function GetDocumentationCommentId() As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function GetDocumentationCommentId() As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function GetDocumentationCommentXml([preferredCulture As CultureInfo = Nothing], [expandIncludes As Boolean = False], [cancellationToken As CancellationToken = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function GetDocumentationCommentXml([preferredCulture As System.Globalization.CultureInfo = Nothing], [expandIncludes As Boolean = False], [cancellationToken As System.Threading.CancellationToken = Nothing]) As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function ToDisplayParts([format As SymbolDisplayFormat = Nothing]) As ImmutableArray(Of SymbolDisplayPart)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function ToDisplayParts([format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SymbolDisplayPart)", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function ToDisplayString([format As SymbolDisplayFormat = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function ToDisplayString([format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function ToMinimalDisplayParts(semanticModel As SemanticModel, position As Integer, [format As SymbolDisplayFormat = Nothing]) As ImmutableArray(Of SymbolDisplayPart)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function ToMinimalDisplayParts(semanticModel As Microsoft.CodeAnalysis.SemanticModel, position As Integer, [format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SymbolDisplayPart)", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function ToMinimalDisplayString(semanticModel As SemanticModel, position As Integer, [format As SymbolDisplayFormat = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Function ToMinimalDisplayString(semanticModel As Microsoft.CodeAnalysis.SemanticModel, position As Integer, [format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property CanBeReferencedByName As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property CanBeReferencedByName As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ContainingAssembly As IAssemblySymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property ContainingAssembly As Microsoft.CodeAnalysis.IAssemblySymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ContainingModule As IModuleSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property ContainingModule As Microsoft.CodeAnalysis.IModuleSymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ContainingNamespace As INamespaceSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property ContainingNamespace As Microsoft.CodeAnalysis.INamespaceSymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ContainingSymbol As ISymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property ContainingSymbol As Microsoft.CodeAnalysis.ISymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ContainingType As INamedTypeSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property ContainingType As Microsoft.CodeAnalysis.INamedTypeSymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property DeclaredAccessibility As Accessibility' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property DeclaredAccessibility As Microsoft.CodeAnalysis.Accessibility", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property DeclaringSyntaxReferences As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SyntaxReference)", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property HasUnsupportedMetadata As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property HasUnsupportedMetadata As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsAbstract As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsAbstract As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsDefinition As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsDefinition As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsExtern As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsExtern As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsImplicitlyDeclared As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsImplicitlyDeclared As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsOverride As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsOverride As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsSealed As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsSealed As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsStatic As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsStatic As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsVirtual As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property IsVirtual As Boolean", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Kind As SymbolKind' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property Kind As Microsoft.CodeAnalysis.SymbolKind", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Language As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property Language As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Locations As ImmutableArray(Of Location)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property Locations As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Location)", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property MetadataName As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property MetadataName As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Name As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property Name As String", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property OriginalDefinition As ISymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "ReadOnly Property OriginalDefinition As Microsoft.CodeAnalysis.ISymbol", "ISymbol"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Sub Accept(visitor As SymbolVisitor)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 46).WithArguments("Class", "Foo", "Sub Accept(visitor As Microsoft.CodeAnalysis.SymbolVisitor)", "ISymbol"),
                        // Test0.vb(6,7): error RS1009: Type Bar cannot implement interface ISymbol because ISymbol is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(6, 7, 6, 10).WithArguments("Bar", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function Accept(Of TResult)(visitor As Microsoft.CodeAnalysis.SymbolVisitor(Of TResult)) As TResult", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function Equals(other As ISymbol) As Boolean' for interface 'IEquatable(Of ISymbol)'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function Equals(other As Microsoft.CodeAnalysis.ISymbol) As Boolean", "IEquatable(Of ISymbol)"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GetAttributes() As ImmutableArray(Of AttributeData)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GetAttributes() As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.AttributeData)", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GetDocumentationCommentId() As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GetDocumentationCommentId() As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GetDocumentationCommentXml([preferredCulture As CultureInfo = Nothing], [expandIncludes As Boolean = False], [cancellationToken As CancellationToken = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GetDocumentationCommentXml([preferredCulture As System.Globalization.CultureInfo = Nothing], [expandIncludes As Boolean = False], [cancellationToken As System.Threading.CancellationToken = Nothing]) As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GetMetadata() As AssemblyMetadata' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GetMetadata() As Microsoft.CodeAnalysis.AssemblyMetadata", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GetTypeByMetadataName(fullyQualifiedMetadataName As String) As INamedTypeSymbol' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GetTypeByMetadataName(fullyQualifiedMetadataName As String) As Microsoft.CodeAnalysis.INamedTypeSymbol", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function GivesAccessTo(toAssembly As IAssemblySymbol) As Boolean' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function GivesAccessTo(toAssembly As Microsoft.CodeAnalysis.IAssemblySymbol) As Boolean", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function ResolveForwardedType(fullyQualifiedMetadataName As String) As INamedTypeSymbol' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function ResolveForwardedType(fullyQualifiedMetadataName As String) As Microsoft.CodeAnalysis.INamedTypeSymbol", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function ToDisplayParts([format As SymbolDisplayFormat = Nothing]) As ImmutableArray(Of SymbolDisplayPart)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function ToDisplayParts([format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SymbolDisplayPart)", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function ToDisplayString([format As SymbolDisplayFormat = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function ToDisplayString([format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function ToMinimalDisplayParts(semanticModel As SemanticModel, position As Integer, [format As SymbolDisplayFormat = Nothing]) As ImmutableArray(Of SymbolDisplayPart)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function ToMinimalDisplayParts(semanticModel As Microsoft.CodeAnalysis.SemanticModel, position As Integer, [format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SymbolDisplayPart)", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function ToMinimalDisplayString(semanticModel As SemanticModel, position As Integer, [format As SymbolDisplayFormat = Nothing]) As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Function ToMinimalDisplayString(semanticModel As Microsoft.CodeAnalysis.SemanticModel, position As Integer, [format As Microsoft.CodeAnalysis.SymbolDisplayFormat = Nothing]) As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property CanBeReferencedByName As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property CanBeReferencedByName As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ContainingAssembly As IAssemblySymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property ContainingAssembly As Microsoft.CodeAnalysis.IAssemblySymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ContainingModule As IModuleSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property ContainingModule As Microsoft.CodeAnalysis.IModuleSymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ContainingNamespace As INamespaceSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property ContainingNamespace As Microsoft.CodeAnalysis.INamespaceSymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ContainingSymbol As ISymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property ContainingSymbol As Microsoft.CodeAnalysis.ISymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ContainingType As INamedTypeSymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property ContainingType As Microsoft.CodeAnalysis.INamedTypeSymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property DeclaredAccessibility As Accessibility' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property DeclaredAccessibility As Microsoft.CodeAnalysis.Accessibility", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property DeclaringSyntaxReferences As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.SyntaxReference)", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property GlobalNamespace As INamespaceSymbol' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property GlobalNamespace As Microsoft.CodeAnalysis.INamespaceSymbol", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property HasUnsupportedMetadata As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property HasUnsupportedMetadata As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Identity As AssemblyIdentity' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Identity As Microsoft.CodeAnalysis.AssemblyIdentity", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsAbstract As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsAbstract As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsDefinition As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsDefinition As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsExtern As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsExtern As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsImplicitlyDeclared As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsImplicitlyDeclared As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsInteractive As Boolean' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsInteractive As Boolean", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsOverride As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsOverride As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsSealed As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsSealed As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsStatic As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsStatic As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsVirtual As Boolean' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property IsVirtual As Boolean", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Kind As SymbolKind' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Kind As Microsoft.CodeAnalysis.SymbolKind", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Language As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Language As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Locations As ImmutableArray(Of Location)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Locations As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Location)", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property MetadataName As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property MetadataName As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property MightContainExtensionMethods As Boolean' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property MightContainExtensionMethods As Boolean", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Modules As IEnumerable(Of IModuleSymbol)' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Modules As System.Collections.Generic.IEnumerable(Of Microsoft.CodeAnalysis.IModuleSymbol)", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Name As String' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property Name As String", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property NamespaceNames As ICollection(Of String)' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property NamespaceNames As System.Collections.Generic.ICollection(Of String)", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property OriginalDefinition As ISymbol' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property OriginalDefinition As Microsoft.CodeAnalysis.ISymbol", "ISymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property TypeNames As ICollection(Of String)' for interface 'IAssemblySymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "ReadOnly Property TypeNames As System.Collections.Generic.ICollection(Of String)", "IAssemblySymbol"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Sub Accept(visitor As SymbolVisitor)' for interface 'ISymbol'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 54).WithArguments("Class", "Bar", "Sub Accept(visitor As Microsoft.CodeAnalysis.SymbolVisitor)", "ISymbol")
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyIOperation()
        {
            var source = @"
' Causes many compile errors, because not all members are implemented.
Class Foo 
    Implements Microsoft.CodeAnalysis.IOperation
End Class
Class Bar
    Implements Microsoft.CodeAnalysis.Operations.IInvocationOperation
End Class
";

            // Verify that IOperation is not implementable.
            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(3,7): error RS1009: Type Foo cannot implement interface IOperation because IOperation is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(3, 7, 3, 10).WithArguments("Foo", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "Function Accept(Of TArgument, TResult)(visitor As Microsoft.CodeAnalysis.Operations.OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Children As IEnumerable(Of IOperation)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Children As System.Collections.Generic.IEnumerable(Of Microsoft.CodeAnalysis.IOperation)", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property ConstantValue As [Optional](Of Object)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property ConstantValue As Microsoft.CodeAnalysis.Optional(Of Object)", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property IsImplicit As Boolean' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property IsImplicit As Boolean", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Kind As OperationKind' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Kind As Microsoft.CodeAnalysis.OperationKind", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Language As String' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Language As String", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Parent As IOperation' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Parent As Microsoft.CodeAnalysis.IOperation", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property SemanticModel As SemanticModel' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property SemanticModel As Microsoft.CodeAnalysis.SemanticModel", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Syntax As SyntaxNode' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Syntax As Microsoft.CodeAnalysis.SyntaxNode", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'ReadOnly Property Type As ITypeSymbol' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "ReadOnly Property Type As Microsoft.CodeAnalysis.ITypeSymbol", "IOperation"),
                        // Test0.vb(4) : error BC30149: Class 'Foo' must implement 'Sub Accept(visitor As OperationVisitor)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(4, 16, 4, 49).WithArguments("Class", "Foo", "Sub Accept(visitor As Microsoft.CodeAnalysis.Operations.OperationVisitor)", "IOperation"),
                        // Test0.vb(6,7): error RS1009: Type Bar cannot implement interface IOperation because IOperation is not available for public implementation.
                        VerifyVB.Diagnostic().WithSpan(6, 7, 6, 10).WithArguments("Bar", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "Function Accept(Of TArgument, TResult)(visitor As Microsoft.CodeAnalysis.Operations.OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Arguments As ImmutableArray(Of IArgumentOperation)' for interface 'IInvocationOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Arguments As System.Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Operations.IArgumentOperation)", "IInvocationOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Children As IEnumerable(Of IOperation)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Children As System.Collections.Generic.IEnumerable(Of Microsoft.CodeAnalysis.IOperation)", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property ConstantValue As [Optional](Of Object)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property ConstantValue As Microsoft.CodeAnalysis.Optional(Of Object)", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Instance As IOperation' for interface 'IInvocationOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Instance As Microsoft.CodeAnalysis.IOperation", "IInvocationOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsImplicit As Boolean' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property IsImplicit As Boolean", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property IsVirtual As Boolean' for interface 'IInvocationOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property IsVirtual As Boolean", "IInvocationOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Kind As OperationKind' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Kind As Microsoft.CodeAnalysis.OperationKind", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Language As String' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Language As String", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Parent As IOperation' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Parent As Microsoft.CodeAnalysis.IOperation", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property SemanticModel As SemanticModel' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property SemanticModel As Microsoft.CodeAnalysis.SemanticModel", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Syntax As SyntaxNode' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Syntax As Microsoft.CodeAnalysis.SyntaxNode", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property TargetMethod As IMethodSymbol' for interface 'IInvocationOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property TargetMethod As Microsoft.CodeAnalysis.IMethodSymbol", "IInvocationOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'ReadOnly Property Type As ITypeSymbol' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "ReadOnly Property Type As Microsoft.CodeAnalysis.ITypeSymbol", "IOperation"),
                        // Test0.vb(7) : error BC30149: Class 'Bar' must implement 'Sub Accept(visitor As OperationVisitor)' for interface 'IOperation'.
                        DiagnosticResult.CompilerError("BC30149").WithSpan(7, 16, 7, 70).WithArguments("Class", "Bar", "Sub Accept(visitor As Microsoft.CodeAnalysis.Operations.OperationVisitor)", "IOperation"),
                    },
                },
            }.RunAsync();
        }
    }
}
