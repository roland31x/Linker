using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Linker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Linker lnk = new Linker();
            lnk.Operate();
            ReadBack();
        }
        static void ReadBack()
        {
            string s = Path.GetFullPath("output");
            Console.WriteLine("Linking process log can be found at: ");
            Console.WriteLine(s);
            Console.WriteLine();
            Console.WriteLine("Reading the from log file:");
            Console.WriteLine();
            using (StreamReader sr = new StreamReader("output"))
            {
                while (!sr.EndOfStream)
                {
                    Console.WriteLine(sr.ReadLine());
                    Thread.Sleep(50);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Process finished...");
            Console.ReadKey();
        }
    }
}
