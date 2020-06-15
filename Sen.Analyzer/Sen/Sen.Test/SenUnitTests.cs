using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Sen;

namespace Sen.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class DemoPlayer : Player<IDemoUnionData>
    {
        Immutable<IDemoUnionData> HandleMessage12(Message message)
        {
            return new Immutable<IDemoUnionData>(message);
        }

        Hand
    }
    class Player<T> where T : IUnionData
    { }
    class Message : IDemoUnionData { }
    interface IUnionData { }
    interface IDemoUnionData : IUnionData { }
    struct Immutable<T>
    {
        public T Data { get; set; }
        public Immutable(T data)
        {
            Data = data;
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Sen",
                Message = String.Format("Method {0} seem likes a message handler of {1}", "HandleMessage12", SenAnalyzer.HandleMessage),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 35)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class DemoPlayer : Player<IDemoUnionData>
    {
        Immutable<IDemoUnionData> HandleMessage(Message message)
        {
            return new Immutable<IDemoUnionData>(message);
        }
    }
    class Player<T> where T : IUnionData
    { }
    class Message : IDemoUnionData { }
    interface IUnionData { }
    interface IDemoUnionData : IUnionData { }
    struct Immutable<T>
    {
        public T Data { get; set; }
        public Immutable(T data)
        {
            Data = data;
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SenCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SenAnalyzer();
        }
    }

    #region Code
    class DemoPlayer : Player<IDemoUnionData>
    {
        Immutable<IDemoUnionData> HandleMessage(Message message)
        {
            return new Immutable<IDemoUnionData>(message);
        }
    }
    class Player<T> where T : IUnionData
    { }
    class Message : IDemoUnionData { }
    interface IUnionData { }
    interface IDemoUnionData : IUnionData { }
    struct Immutable<T>
    {
        public T Data { get; set; }
        public Immutable(T data)
        {
            Data = data;
        }
    }
    #endregion
}
