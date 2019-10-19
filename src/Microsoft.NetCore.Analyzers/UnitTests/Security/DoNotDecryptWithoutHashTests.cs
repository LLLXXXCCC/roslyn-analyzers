using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    public class DoNotDecryptWithoutHashTests : TaintedDataAnalyzerTestBase
    {
        public DoNotDecryptWithoutHashTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override DiagnosticDescriptor Rule => DoNotDecryptWithoutHash.Rule;

        [Fact]
        public void TestConstructorWithStoreNameParameterDiagnostic()
        {
            VerifyCSharp(@"
using System.IO;
using System.Security.Cryptography;

class TestClass
{
    public void TestMethod(byte[] buffer1, int offset, int count, Stream stream, CryptoStreamMode mode)
    {
        var decryptor = new AesCng().CreateDecryptor(); 
        var cryptoStream = new CryptoStream(stream, decryptor, mode);
        cryptoStream.Write(buffer1, offset, count);
    }
}",
            GetCSharpResultAt(11, 9, 11, 28, "ICryptoTransform SymmetricAlgorithm.CreateEncryptor(byte[] rgbKey, byte[] rgbIV)", "void TestClass.TestMethod(byte[] someOtherBytesForIV)", "byte[] Convert.FromBase64String(string s)", "void TestClass.TestMethod(byte[] someOtherBytesForIV)"));
        }

        [Fact]
        public void TestConstructorWithStoreNameParameterNoDiagnostic()
        {
            VerifyCSharp(@"
using System.IO;
using System.Security.Cryptography;

class TestClass
{
    public void TestMethod(byte[] buffer, int offset, int count, Stream stream, CryptoStreamMode mode)
    {
        var decryptor = new AesCng().CreateDecryptor(); 
        var cryptoStream = new CryptoStream(stream, decryptor, mode);
        HashAlgorithm sha = new SHA1CryptoServiceProvider();
        byte[] result = sha.ComputeHash(buffer);
        cryptoStream.Write(buffer, offset, count);
    }
}");
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DoNotDecryptWithoutHash();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DoNotDecryptWithoutHash();
        }
    }
}
