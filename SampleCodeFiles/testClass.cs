using System;
using MongoDB.Driver;
namespace MongoDB_Analyzer
{
    public class testClass
    {
        private const int instanceVariableOne = 10;
        public testClass()
        {
        }

        static void MainMethod(string[] args)
        {
            secondMethod(1, 2, 3);
        }

        static void secondMethod(int value, int value2, int value3)
        {
            int x = value + value2 + value3;
        }

        static int thirdMethod(int x, bool check)
        {

            if (check)
            {
                return x + 5;
            }
            return x + 1;
        }

        public void Method()
        {
            var x = 1;
            var y = x + 123;
            var z = y + 132;
            var w = z + z;
            x = 10 * w;
            y = x + 5;

            z = 45;

            var p = x + y + z;
        }

    }
}