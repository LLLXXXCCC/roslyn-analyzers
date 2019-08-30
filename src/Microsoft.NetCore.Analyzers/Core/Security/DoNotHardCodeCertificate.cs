//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using Analyzer.Utilities;
//using Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.NetCore.Analyzers.Security.Helpers;

//namespace Microsoft.NetCore.Analyzers.Security
//{
//    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
//    public class DoNotHardCodeCertificate : SourceTriggeredTaintedDataAnalyzerBase
//    {
//        internal static DiagnosticDescriptor Rule = SecurityHelpers.CreateDiagnosticDescriptor(
//            "CA5393",
//            nameof(MicrosoftNetCoreAnalyzersResources.DoNotHardCodeCertificate),
//            nameof(MicrosoftNetCoreAnalyzersResources.DoNotHardCodeCertificateMessage),
//            DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
//            helpLinkUri: null,
//            descriptionResourceStringName: nameof(MicrosoftNetCoreAnalyzersResources.DoNotHardCodeCertificateDescription),
//            customTags: WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry);

//        protected override SinkKind SinkKind { get { return SinkKind.HardcodedCertificate; } }

//        protected override DiagnosticDescriptor TaintedDataEnteringSinkDescriptor { get { return Rule; } }

//        internal override void ReportDiagnostic(OperationBlockAnalysisContext operationBlockAnalysisContext, TaintedDataAnalysisResult taintedDataAnalysisResult)
//        {

//            foreach (TaintedDataSourceSink sourceSink in taintedDataAnalysisResult.TaintedDataSourceSinks)
//            {
//                if (!sourceSink.SinkKinds.Contains(this.SinkKind))
//                {
//                    continue;
//                }

//                foreach (SymbolAccess sourceOrigin in sourceSink.SourceOrigins)
//                {
//                    // Something like:
//                    // CA3001: Potential SQL injection vulnerability was found where '{0}' in method '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
//                    Diagnostic diagnostic = Diagnostic.Create(
//                        this.TaintedDataEnteringSinkDescriptor,
//                        sourceSink.Sink.Location,
//                        additionalLocations: new Location[] { sourceOrigin.Location },
//                        messageArgs: new object[] {
//                                                        sourceSink.Sink.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
//                                                        sourceSink.Sink.AccessingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
//                                                        sourceOrigin.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
//                                                        sourceOrigin.AccessingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)});
//                    operationBlockAnalysisContext.ReportDiagnostic(diagnostic);
//                }
//            }
//        }
//    }
//}
