using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleTester
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            ModeConCols();
            // 
            PressAnyKey();
        }
        private static void PressAnyKey()
        {
            Console.WriteLine();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ModeConCols()
        {
            Console.SetWindowSize(160, 50);
        }
    }
}
