// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class DecryptWithoutHashSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded certificate tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static DecryptWithoutHashSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                taintArray: ArrayKind.All);
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemSecurityCryptographySymmetricAlgorithm,
                isInterface: false,
                taintedProperties: null,
                taintedMethods: new[] {
                    "CreateDecryptor",
                });

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
