// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedCertificateSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded key tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static HardcodedCertificateSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintedMethodsNeedsTaintedDataAnalysis: null,
                taintConstantArray: true);
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemIOFileFullName,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintedMethodsNeedsTaintedDataAnalysis:
                    new (string, IsInvocationTaintedWithTaintedDataAnalysis)[]{
                        ("FromBase64String",
                        (IEnumerable<TaintedDataAbstractValue> argumentTaintedDatas) => argumentTaintedDatas.All(o => o.Kind == TaintedDataAbstractValueKind.Tainted)),
                    },
                taintConstantArray: true);

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
