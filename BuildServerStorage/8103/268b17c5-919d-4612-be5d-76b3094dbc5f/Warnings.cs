using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warnings
{
    class Warnings : ITest
    {
        static void Main(string[] args)
        {
            int a = 0;
            int b = 0;
            int c = 0;
            int d = 0;
            int e = 0;
            //  Havent used this variable.
        }

        public bool Test()
        {
            Console.WriteLine("Test case executing.");
            throw new Exception("Throwing this exception from test case for demo purpose.");
            return (new object().GetHashCode() % 2 == 0);
        }

        void A()
        {

        }
    }

    public interface ITest
    {
        bool Test();
    }
}
