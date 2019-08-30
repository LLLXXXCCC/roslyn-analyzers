//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//namespace Microsoft.NetCore.Analyzers.Security.UnitTests
//{
//    public class DoNotDecryptWithoutComputingHashTests : DiagnosticAnalyzerTestBase
//    {
//        [Fact]
//        public void TestConstructorWithStoreNameParameterDiagnostic()
//        {
//            VerifyCSharp(@"
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod(ICryptoTransform transform, CryptoStreamMode mode)
//    {
//        var decryptor= new SymmetricAlgorithm().CreateDecryptor(); 
//        var cryptoStream = new Stream(decryptor, transform, mode);
//        cryptoStream.Write();
//    }
//}",
//            GetCSharpResultAt(10, 9, DoNotDecryptWithoutComputingHash.DefinitelyInstallRootCertRule));
//        }

//        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
//        {
//            return new DoNotDecryptWithoutComputingHash();
//        }

//        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
//        {
//            return new DoNotDecryptWithoutComputingHash();
//        }
//    }
//}
