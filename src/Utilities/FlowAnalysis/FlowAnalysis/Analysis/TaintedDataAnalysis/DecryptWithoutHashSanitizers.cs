// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class DecryptWithoutHashSanitizers
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for information disclosure tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static DecryptWithoutHashSanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemSecurityCryptographyHashAlgorithmName,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: null,
                sanitizingInstanceMethods: null,
                sanitizingArgumentMethods: new[] {
                    "ComputeHash",
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
