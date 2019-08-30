﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Test.Utilities.MinimalImplementations;
using Xunit;

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.PropertySetAnalysis)]
    public class DoNotDisableHttpClientCRLCheckTests : DiagnosticAnalyzerTestBase
    {
        private static readonly DiagnosticDescriptor DefinitelyRule = DoNotDisableHttpClientCRLCheck.DefinitelyDisableHttpClientCRLCheckRule;
        private static readonly DiagnosticDescriptor MaybeRule = DoNotDisableHttpClientCRLCheck.MaybeDisableHttpClientCRLCheckRule;

        protected void VerifyCSharpWithDependencies(string source, params DiagnosticResult[] expected)
        {
            this.VerifyCSharp(
                new[] { source, SystemNetHttpApis.CSharp }.ToFileAndSource(),
                expected);
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_NotSet_DefaultWrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(9, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_Wrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(10, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_HttpClientHandler_CheckCertificateRevocationList_Wrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient(httpClientHandler);
    }
}",
                GetCSharpResultAt(10, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_CurlHandler_CheckCertificateRevocationList_Wrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;
using System.Net.Http.Unix;

class TestClass
{
    void TestMethod()
    {
        var curlHandler = new CurlHandler();
        curlHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient(curlHandler);
    }
}",
                GetCSharpResultAt(11, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_HttpClientWithHttpMessageHandlerAndBooleanParameters_WinHttpHandler_CheckCertificateRevocationList_Wrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod(bool disposeHandler)
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient(winHttpHandler, disposeHandler);
    }
}",
                GetCSharpResultAt(10, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_WinHttpHandler_PropertyInitializer_CheckCertificateRevocationList_Wrong_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler() { CheckCertificateRevocationList = false };
        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(9, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_HttpClientConstructorWithoutParameter_handlerSetByDefault_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient();
    }
}",
                GetCSharpResultAt(10, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_UnknownOrRight_MaybeDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System;
using System.Net.Http;

class TestClass
{
    void TestMethod(bool checkCertificateRevocationList)
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = checkCertificateRevocationList;
        Random r = new Random();

        if (r.Next(6) == 4)
        {
            winHttpHandler.CheckCertificateRevocationList = true;
        }

        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(18, 26, MaybeRule));
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_UnknownOrWrong_MaybeDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System;
using System.Net.Http;

class TestClass
{
    void TestMethod(bool checkCertificateRevocationList)
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = checkCertificateRevocationList;
        Random r = new Random();

        if (r.Next(6) == 4)
        {
            winHttpHandler.CheckCertificateRevocationList = false;
        }

        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(18, 26, MaybeRule));
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_WrongOrRight_MaybeDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System;
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = false;
        Random r = new Random();

        if (r.Next(6) == 4)
        {
            winHttpHandler.CheckCertificateRevocationList = true;
        }

        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(18, 26, MaybeRule));
        }

        [Fact]
        public void Test_DerivedClassOfHttpClient_DefinitelyDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class DerivedClass : HttpClient
{
    public DerivedClass()
    {
    }
    
    public DerivedClass(HttpMessageHandler handler)
    {
    }

    public DerivedClass(HttpMessageHandler handler, bool disposeHandler)
    {
    }
}

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = false;
        var httpClient = new HttpClient(winHttpHandler);
    }
}",
                GetCSharpResultAt(10, 26, DefinitelyRule));
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_Right_NoDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod()
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = true;
        var httpClient = new HttpClient(winHttpHandler);
    }
}");
        }

        [Fact]
        public void Test_WinHttpHandler_CheckCertificateRevocationList_Unknown_NoDiagnostic()
        {
            this.VerifyCSharpWithDependencies(@"
using System.Net.Http;

class TestClass
{
    void TestMethod(bool checkCertificateRevocationList)
    {
        var winHttpHandler = new WinHttpHandler();
        winHttpHandler.CheckCertificateRevocationList = checkCertificateRevocationList;
        var httpClient = new HttpClient(winHttpHandler);
    }
}");
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DoNotDisableHttpClientCRLCheck();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DoNotDisableHttpClientCRLCheck();
        }
    }
}
