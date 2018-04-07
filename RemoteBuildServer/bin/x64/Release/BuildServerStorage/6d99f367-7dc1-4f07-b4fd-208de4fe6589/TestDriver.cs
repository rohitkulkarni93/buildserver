using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHarnessSDK;

namespace TestBuild
{
    [TestSuite(TestSuiteName = "TestOneAndTestTwo")]
    public class TestDriver : ITest
    {
        public TestDriver() { }

        public bool Test()
        {
            TestedOne one = new TestedOne();
            one.sayOne();
            TestedTwo two = new TestedTwo();
            two.sayTwo();
            return true;  // just pretending to test something
        }
        static void Main(string[] args)
        {
            Console.Write("\n  TestDriver running:");
            TestDriver td = new TestDriver();
            td.Test();
            Console.Write("\n\n");
        }
    }
}
