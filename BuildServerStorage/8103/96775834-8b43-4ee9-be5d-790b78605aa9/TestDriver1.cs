using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warnings
{
    class TestDriver1 : ITest
    {

        public bool Test()
        {
            Console.WriteLine("This tests the calculator for basic function.");
            Calculator calculator = new Calculator();
            int result = calculator.Add(3, 4);
            return result == 7;
        }
    }
}
