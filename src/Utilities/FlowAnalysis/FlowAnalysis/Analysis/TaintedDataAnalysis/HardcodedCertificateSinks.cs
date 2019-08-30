// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedCertificateSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data process symmetric algorithm sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static HardcodedCertificateSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFileFullName,
                SinkKind.HardcodedCertificate,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "WriteAllBytes", new[] { "bytes" }),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemSecurityCryptographyX509CertificatesX509Certificate,
                SinkKind.HardcodedCertificate,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: null);

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
