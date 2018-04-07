using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Errors
{
    class Errors : ITest
    {
        static void Main(string[] args)
        {
            int a = 4;
            
        }
    }

    interface ITest
    {
        void Test();
    }
}
