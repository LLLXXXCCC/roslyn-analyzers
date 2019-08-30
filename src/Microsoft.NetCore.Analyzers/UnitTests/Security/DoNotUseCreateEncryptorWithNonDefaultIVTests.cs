//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.Testing;
//using Test.Utilities;
//using Xunit;

//namespace Microsoft.NetCore.Analyzers.Security.UnitTests
//{
//    public class DoNotUseCreateEncryptorWithNonDefaultIVTests : DiagnosticAnalyzerTestBase
//    {
//        [Fact]
//        public void Test_CreateEncryptor_DefaultIV_NoDiagnostic()
//        {
//            VerifyCSharp(@"
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod()
//    {
//        var aesCng  = new AesCng();
//        aesCng.CreateEncryptor();
//    }
//}");
//        }

//        [Fact]
//        public void Test_CreateEncryptorWithByteArrayAndByteArrayParameters_NonDefaultIV_NoDiagnostic()
//        {
//            VerifyCSharp(@"
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod(byte[] rgbKey, byte[] rgbIV)
//    {
//        var aesCng  = new AesCng();
//        aesCng.IV = rgbIV;
//        aesCng.CreateEncryptor(rgbKey, rgbIV);
//    }
//}");
//        }

//        [Fact]
//        public void Test_CreateEncryptor_NonDefaultIV_MaybeNull_Diagnostic()
//        {
//            VerifyCSharp(@"
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod(byte[] rgbIV)
//    {
//        var aesCng  = new AesCng();
//        aesCng.IV = rgbIV;
//        aesCng.CreateEncryptor();
//    }
//}",
//            GetCSharpResultAt(10, 9, DoNotUseCreateEncryptorWithNonDefaultIV.MaybeUseCreateEncryptorWithNonDefaultIVRule));
//        }

//        [Fact]
//        public void Test_CreateEncryptor_NonDefaultIV_MaybeSetWithMaybeNull_Diagnostic()
//        {
//            VerifyCSharp(@"
//using System;
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod(byte[] rgbIV)
//    {
//        var aesCng  = new AesCng();
//        Random r = new Random();

//        if (r.Next(6) == 4)
//        {
//            aesCng.IV = rgbIV;
//        }

//        aesCng.CreateEncryptor();
//    }
//}",
//            GetCSharpResultAt(10, 9, DoNotUseCreateEncryptorWithNonDefaultIV.MaybeUseCreateEncryptorWithNonDefaultIVRule));
//        }

//        [Fact]
//        public void Test_CreateEncryptor_NonDefaultIV_NotNull_Diagnostic()
//        {
//            VerifyCSharp(@"
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod()
//    {
//        byte[] rgbIV = new byte[] { 1, 2, 3};
//        var aesCng  = new AesCng();
//        aesCng.IV = rgbIV;
//        aesCng.CreateEncryptor();
//    }
//}",
//            GetCSharpResultAt(10, 9, DoNotUseCreateEncryptorWithNonDefaultIV.DefinitelyUseCreateEncryptorWithNonDefaultIVRule));
//        }

//        [Fact]
//        public void Test_CreateEncryptor_NonDefaultIV_MaybeSetNotNull_Diagnostic()
//        {
//            VerifyCSharp(@"
//using System;
//using System.Security.Cryptography;

//class TestClass
//{
//    public void TestMethod()
//    {
//        byte[] rgbIV = new byte[] { 1, 2, 3};
//        var aesCng  = new AesCng();
//        Random r = new Random();

//        if (r.Next(6) == 4)
//        {
//            aesCng.IV = rgbIV;
//        }

//        aesCng.CreateEncryptor();
//    }
//}",
//            GetCSharpResultAt(10, 9, DoNotUseCreateEncryptorWithNonDefaultIV.MaybeUseCreateEncryptorWithNonDefaultIVRule));
//        }

//        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
//        {
//            return new DoNotUseCreateEncryptorWithNonDefaultIV();
//        }

//        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
//        {
//            return new DoNotUseCreateEncryptorWithNonDefaultIV();
//        }
//    }
//}
