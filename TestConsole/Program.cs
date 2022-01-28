using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Console App");

            char[] s = new[] {(char)152, (char)38, (char)0, (char)0};
            
            string sendStr = new string(s);

            byte[] bytes = Encoding.ASCII.GetBytes(sendStr);
         
            Console.ReadLine();
        }
    }
}
