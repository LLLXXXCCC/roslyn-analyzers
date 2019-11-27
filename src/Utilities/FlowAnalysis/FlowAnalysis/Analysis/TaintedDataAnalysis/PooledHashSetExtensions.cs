﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class PooledHashSetExtensions
    {
        // Just to make hardcoding SinkInfos more convenient.
        public static void AddSinkInfo(
            this PooledHashSet<SinkInfo> builder,
            string fullTypeName,
            SinkKind sinkKind,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string>? sinkProperties,
            IEnumerable<(string Method, string[] Parameters)>? sinkMethodParameters,
            IEnumerable<(string Method, string[] Parameters)>? sinkMethodParametersWithTaintedInstance = null)
        {
            builder.AddSinkInfo(
                fullTypeName,
                new[] { sinkKind },
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties,
                sinkMethodParameters,
                sinkMethodParametersWithTaintedInstance);
        }

        // Just to make hardcoding SinkInfos more convenient.
        public static void AddSinkInfo(
            this PooledHashSet<SinkInfo> builder,
            string fullTypeName,
            IEnumerable<SinkKind> sinkKinds,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string>? sinkProperties,
            IEnumerable<(string Method, string[] Parameters)>? sinkMethodParameters,
            IEnumerable<(string Method, string[] Parameters)>? sinkMethodParametersWithTaintedInstance = null)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                sinkKinds.ToImmutableHashSet(),
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties: sinkProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                        ?? ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters
                            ?.Select(o => new KeyValuePair<string, ImmutableHashSet<string>>(o.Method, o.Parameters.ToImmutableHashSet()))
                            ?.ToImmutableDictionary(StringComparer.Ordinal)
                        ?? ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
                sinkMethodParametersWithTaintedInstance:
                    sinkMethodParametersWithTaintedInstance
                            ?.Select(o => new KeyValuePair<string, ImmutableHashSet<string>>(o.Method, o.Parameters.ToImmutableHashSet()))
                            ?.ToImmutableDictionary(StringComparer.Ordinal)
                        ?? ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            builder.Add(sinkInfo);
        }

        // Just to make hardcoding SourceInfos more convenient.
        public static void AddSourceInfo(
            this PooledHashSet<SourceInfo> builder,
            string fullTypeName,
            bool isInterface,
            string[]? taintedProperties,
            IEnumerable<string>? taintedMethods)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethods:
                    taintedMethods
                        ?.Select<string, (MethodMatcher, ImmutableHashSet<string>)>(o =>
                            (
                                (methodName, arguments) => methodName == o,
                                ImmutableHashSet<string>.Empty.Add(TaintedTargetValue.Return)
                            ))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<string>)>.Empty,
                taintedMethodsNeedsPointsToAnalysis:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(PointsToCheck, string)>)>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(ValueContentCheck, string)>)>.Empty,
                transferMethods:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(string, string)>)>.Empty,
                taintArray: TaintArrayKind.None);
            builder.Add(metadata);
        }

        /// <summary>
        /// Add SourceInfos which needs extra PointsToAnalysis checks or ValueContentAnalysis checks and specifies the tainted targets explicitly for each check.
        /// The tainted targets can be parameter names of the method, or the return value.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="fullTypeName"></param>
        /// <param name="taintedProperties"></param>
        /// <param name="taintedMethodsNeedsPointsToAnalysis">Specify the check functions and tainted targets for methods which only need PointsToAnalysis check.</param>
        /// <param name="taintedMethodsNeedsValueContentAnalysis">Specify the check functions and tainted targets for methods which need ValueContentAnalysis check.</param>
        /// <param name="taintArray">Specify whether to taint array.</param>
        public static void AddSourceInfoSpecifyingTaintedTargets(
        this PooledHashSet<SourceInfo> builder,
        string fullTypeName,
        bool isInterface,
        string[]? taintedProperties,
        IEnumerable<(MethodMatcher methodMatcher, (PointsToCheck pointsToCheck, string taintedTarget)[] pointsToChecksAndTargets)>? taintedMethodsNeedsPointsToAnalysis,
        IEnumerable<(MethodMatcher methodMatcher, (ValueContentCheck valueContentCheck, string taintedTarget)[] valueContentChecksAndTargets)>? taintedMethodsNeedsValueContentAnalysis,
        IEnumerable<(MethodMatcher methodMatcher, (string str, string taintedTargets)[] valueContentChecksAndTargets)>? transferMethods,
        TaintArrayKind taintArray = TaintArrayKind.None)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethods:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<string>)>.Empty,
                taintedMethodsNeedsPointsToAnalysis:
                    taintedMethodsNeedsPointsToAnalysis?.Select(o =>
                            (
                                o.methodMatcher,
                                o.pointsToChecksAndTargets?.ToImmutableHashSet()
                                    ?? ImmutableHashSet<(PointsToCheck, string)>.Empty
                            ))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(PointsToCheck, string)>)>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    taintedMethodsNeedsValueContentAnalysis?.Select(o =>
                            (
                                o.methodMatcher,
                                o.valueContentChecksAndTargets?.ToImmutableHashSet()
                                    ?? ImmutableHashSet<(ValueContentCheck, string)>.Empty
                            ))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(ValueContentCheck, string)>)>.Empty,
                transferMethods:
                    transferMethods
                        ?.Select(o =>
                            (
                                o.methodMatcher,
                                o.valueContentChecksAndTargets
                                    ?.ToImmutableHashSet()
                                ?? ImmutableHashSet<(string, string)>.Empty))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(string, string)>)>.Empty,
                taintArray: taintArray);
            builder.Add(metadata);
        }

        /// <summary>
        /// Add SourceInfos which needs PointsToAnalysis checks or ValueContentAnalysis checks and each check taints return value by default.
        /// </summary>
        public static void AddSourceInfo(
            this PooledHashSet<SourceInfo> builder,
            string fullTypeName,
            bool isInterface,
            string[]? taintedProperties,
            IEnumerable<(MethodMatcher methodMatcher, PointsToCheck[] pointsToChecks)>? taintedMethodsNeedsPointsToAnalysis,
            IEnumerable<(MethodMatcher methodMatcher, ValueContentCheck[] valueContentChecks)>? taintedMethodsNeedsValueContentAnalysis,
            TaintArrayKind taintArray = TaintArrayKind.None)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethods:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<string>)>.Empty,
                taintedMethodsNeedsPointsToAnalysis:
                    taintedMethodsNeedsPointsToAnalysis?.Select(o =>
                            (
                                o.methodMatcher,
                                o.pointsToChecks
                                    ?.Select(s => (s, TaintedTargetValue.Return))
                                    ?.ToImmutableHashSet()
                                ?? ImmutableHashSet<(PointsToCheck, string)>.Empty
                            ))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(PointsToCheck, string)>)>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    taintedMethodsNeedsValueContentAnalysis?.Select(o =>
                            (
                                o.methodMatcher,
                                o.valueContentChecks
                                    ?.Select(s => (s, TaintedTargetValue.Return))
                                    ?.ToImmutableHashSet()
                                ?? ImmutableHashSet<(ValueContentCheck, string)>.Empty
                            ))
                        ?.ToImmutableHashSet()
                    ?? ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(ValueContentCheck, string)>)>.Empty,
                transferMethods:
                    ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(string, string)>)>.Empty,
                taintArray: taintArray);
            builder.Add(metadata);
        }

        // Just to make hardcoding SanitizerInfos more convenient.
        public static void AddSanitizerInfo(
            this PooledHashSet<SanitizerInfo> builder,
            string fullTypeName,
            bool isInterface,
            bool isConstructorSanitizing,
            string[]? sanitizingMethods,
            IEnumerable<(string Method, (bool SanitizeReturn, bool SanitizeInstance, string[]? SanitizedArguments) SanitizedTargets)>? sanitizingMethodsSpecifyTargets = null)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isInterface: isInterface,
                isConstructorSanitizing: isConstructorSanitizing,
                sanitizingMethods:
                    (sanitizingMethods
                        ?.Select(o => new KeyValuePair<string, (bool, bool, ImmutableHashSet<string>)>(o, (true, false, ImmutableHashSet<string>.Empty)))
                        ?.ToImmutableDictionary(StringComparer.Ordinal)
                    ?? ImmutableDictionary<string, (bool, bool, ImmutableHashSet<string>)>.Empty).AddRange(
                    sanitizingMethodsSpecifyTargets
                        ?.Select(o =>
                            new KeyValuePair<string, (bool, bool, ImmutableHashSet<string>)>(
                                o.Method,
                                (o.SanitizedTargets.SanitizeReturn, o.SanitizedTargets.SanitizeInstance, o.SanitizedTargets.SanitizedArguments?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty)))
                        ?.ToImmutableDictionary(StringComparer.Ordinal)
                    ?? ImmutableDictionary<string, (bool, bool, ImmutableHashSet<string>)>.Empty));
            builder.Add(info);
        }

        // Just to make hardcoding SanitizerInfos more convenient.
        public static void AddSanitizerInfo(
            this PooledHashSet<SanitizerInfo> builder,
            string fullTypeName,
            bool isInterface,
            bool isConstructorSanitizing,
            IEnumerable<(string Method, (bool SanitizeReturn, bool SanitizeInstance, string[] SanitizedArguments) SanitizedTargets)>? sanitizingMethodsSpecifyTargets)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isInterface: isInterface,
                isConstructorSanitizing: isConstructorSanitizing,
                sanitizingMethods: sanitizingMethodsSpecifyTargets
                            ?.Select(o =>
                                new KeyValuePair<string, (bool, bool, ImmutableHashSet<string>)>(
                                    o.Method,
                                    (o.SanitizedTargets.SanitizeReturn, o.SanitizedTargets.SanitizeInstance, o.SanitizedTargets.SanitizedArguments?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty)))
                            ?.ToImmutableDictionary(StringComparer.Ordinal)
                        ?? ImmutableDictionary<string, (bool, bool, ImmutableHashSet<string>)>.Empty);
            builder.Add(info);
        }
    }
}