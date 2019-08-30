﻿//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Linq;
//using Analyzer.Utilities;
//using Analyzer.Utilities.Extensions;
//using Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis;
//using Analyzer.Utilities.PooledObjects;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
//using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
//using Microsoft.CodeAnalysis.Operations;
//using Microsoft.NetCore.Analyzers.Security.Helpers;

//namespace Microsoft.NetCore.Analyzers.Security
//{
//    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
//    public sealed class DoNotUseCreateEncryptorWithNonDefaultIV : DiagnosticAnalyzer
//    {
//        internal static DiagnosticDescriptor DefinitelyUseCreateEncryptorWithNonDefaultIVRule = SecurityHelpers.CreateDiagnosticDescriptor(
//            "CA5396",
//            typeof(MicrosoftNetCoreAnalyzersResources),
//            nameof(MicrosoftNetCoreAnalyzersResources.DefinitelyUseCreateEncryptorWithNonDefaultIV),
//            nameof(MicrosoftNetCoreAnalyzersResources.DefinitelyUseCreateEncryptorWithNonDefaultIVMessage),
//            DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
//            helpLinkUri: null,
//            descriptionResourceStringName: nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseCreateEncryptorWithNonDefaultIVDescription),
//            customTags: WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry);
//        internal static DiagnosticDescriptor MaybeUseCreateEncryptorWithNonDefaultIVRule = SecurityHelpers.CreateDiagnosticDescriptor(
//            "CA5397",
//            typeof(MicrosoftNetCoreAnalyzersResources),
//            nameof(MicrosoftNetCoreAnalyzersResources.MaybeUseCreateEncryptorWithNonDefaultIV),
//            nameof(MicrosoftNetCoreAnalyzersResources.MaybeUseCreateEncryptorWithNonDefaultIVMessage),
//            DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
//            helpLinkUri: null,
//            descriptionResourceStringName: nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseCreateEncryptorWithNonDefaultIVDescription),
//            customTags: WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry);

//        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
//                                                                                        DefinitelyUseCreateEncryptorWithNonDefaultIVRule,
//                                                                                        MaybeUseCreateEncryptorWithNonDefaultIVRule);

//        private static readonly ConstructorMapper ConstructorMapper = new ConstructorMapper(
//            (IMethodSymbol constructorMethod, IReadOnlyList<PointsToAbstractValue> argumentPointsToAbstractValues) =>
//            {
//                return PropertySetAbstractValue.GetInstance(PropertySetAbstractValueKind.Unflagged);
//            });

//        private static readonly PropertyMapperCollection PropertyMappers = new PropertyMapperCollection(
//            new PropertyMapper(
//                "IV",
//                PropertySetCallbacks.FlagIfNotNull));

//        private static readonly HazardousUsageEvaluatorCollection HazardousUsageEvaluators = new HazardousUsageEvaluatorCollection(
//            new HazardousUsageEvaluator(
//                    "CreateEncryptor",
//                    PropertySetCallbacks.HazardousIfAllFlaggedAndAtLeastOneKnown));

//        public override void Initialize(AnalysisContext context)
//        {
//            context.EnableConcurrentExecution();

//            // Security analyzer - analyze and report diagnostics on generated code.
//            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

//            context.RegisterCompilationStartAction(
//                (CompilationStartAnalysisContext compilationStartAnalysisContext) =>
//                {
//                    var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilationStartAnalysisContext.Compilation);

//                    if (!wellKnownTypeProvider.TryGetTypeByMetadataName(WellKnownTypeNames.SystemSecurityCryptographySymmetricAlgorithm, out var symmetricAlgorithmTypeSymbol))
//                    {
//                        return;
//                    }

//                    var rootOperationsNeedingAnalysis = PooledHashSet<(IOperation, ISymbol)>.GetInstance();

//                    compilationStartAnalysisContext.RegisterOperationBlockStartAction(
//                        (OperationBlockStartAnalysisContext operationBlockStartAnalysisContext) =>
//                        {
//                            var owningSymbol = operationBlockStartAnalysisContext.OwningSymbol;

//                            // TODO: Handle case when exactly one of the below rules is configured to skip analysis.
//                            if (owningSymbol.IsConfiguredToSkipAnalysis(operationBlockStartAnalysisContext.Options,
//                                    DefinitelyUseCreateEncryptorWithNonDefaultIVRule, operationBlockStartAnalysisContext.Compilation, operationBlockStartAnalysisContext.CancellationToken) &&
//                                owningSymbol.IsConfiguredToSkipAnalysis(operationBlockStartAnalysisContext.Options,
//                                    MaybeUseCreateEncryptorWithNonDefaultIVRule, operationBlockStartAnalysisContext.Compilation, operationBlockStartAnalysisContext.CancellationToken))
//                            {
//                                return;
//                            }

//                            operationBlockStartAnalysisContext.RegisterOperationAction(
//                                (OperationAnalysisContext operationAnalysisContext) =>
//                                {
//                                    var invocationOperation = (IInvocationOperation)operationAnalysisContext.Operation;

//                                    if (symmetricAlgorithmTypeSymbol.Equals(invocationOperation.Instance?.Type) &&
//                                        invocationOperation.TargetMethod.Name == "CreateEncryptor")
//                                    {
//                                        lock (rootOperationsNeedingAnalysis)
//                                        {
//                                            rootOperationsNeedingAnalysis.Add((invocationOperation.GetRoot(), operationAnalysisContext.ContainingSymbol));
//                                        }
//                                    }
//                                },
//                                OperationKind.Invocation);
//                        });

//                    compilationStartAnalysisContext.RegisterCompilationEndAction(
//                        (CompilationAnalysisContext compilationAnalysisContext) =>
//                        {
//                            PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> allResults = null;

//                            try
//                            {
//                                lock (rootOperationsNeedingAnalysis)
//                                {
//                                    if (!rootOperationsNeedingAnalysis.Any())
//                                    {
//                                        return;
//                                    }

//                                    allResults = PropertySetAnalysis.BatchGetOrComputeHazardousUsages(
//                                        compilationAnalysisContext.Compilation,
//                                        rootOperationsNeedingAnalysis,
//                                        compilationAnalysisContext.Options,
//                                        WellKnownTypeNames.SystemSecurityCryptographySymmetricAlgorithm,
//                                        ConstructorMapper,
//                                        PropertyMappers,
//                                        HazardousUsageEvaluators,
//                                        InterproceduralAnalysisConfiguration.Create(
//                                            compilationAnalysisContext.Options,
//                                            SupportedDiagnostics,
//                                            defaultInterproceduralAnalysisKind: InterproceduralAnalysisKind.ContextSensitive,
//                                            cancellationToken: compilationAnalysisContext.CancellationToken));
//                                }

//                                if (allResults == null)
//                                {
//                                    return;
//                                }

//                                foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp
//                                    in allResults)
//                                {
//                                    DiagnosticDescriptor descriptor;
//                                    switch (kvp.Value)
//                                    {
//                                        case HazardousUsageEvaluationResult.Flagged:
//                                            descriptor = DefinitelyUseCreateEncryptorWithNonDefaultIVRule;
//                                            break;

//                                        case HazardousUsageEvaluationResult.MaybeFlagged:
//                                            descriptor = MaybeUseCreateEncryptorWithNonDefaultIVRule;
//                                            break;

//                                        default:
//                                            Debug.Fail($"Unhandled result value {kvp.Value}");
//                                            continue;
//                                    }

//                                    compilationAnalysisContext.ReportDiagnostic(
//                                        Diagnostic.Create(
//                                            descriptor,
//                                            kvp.Key.Location,
//                                            kvp.Key.Method.ToDisplayString(
//                                                SymbolDisplayFormat.MinimallyQualifiedFormat)));
//                                }
//                            }
//                            finally
//                            {
//                                rootOperationsNeedingAnalysis.Free();
//                                allResults?.Free();
//                            }
//                        });

//                });
//        }
//    }
//}
