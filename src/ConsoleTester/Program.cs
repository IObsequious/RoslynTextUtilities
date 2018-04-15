using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleTester
{
    internal static class Program
    {
        public static void Main()
        {
            ModeConCols();

            //const string example = "'\\x28'";

            //string value = TextUtilities.GetValueText(example);

            //Console.WriteLine();
            //Console.WriteLine($"Converting {example} to value {value}");
            //Console.WriteLine();

            string text = "Line1\r\nLine2\r\n\u03B5";

            byte[] bytes = Encoding.GetEncoding("windows-1252").GetBytes(text);

            CharStream stream = new CharStream();

            //stream.Write(bytes, 0, bytes.Length);
            //stream.Seek(0, SeekOrigin.Begin);

            SourceTextWriter writer = SourceTextWriter.Create(Encoding.GetEncoding("windows-1252"), SourceHashAlgorithm.Sha1, int.MaxValue);

            writer.Write("Line1");

            string value = writer.ToSourceText().ToString();

            //char c0 = stream.ReadChar();
            //char c1 = stream.ReadChar();
            //char c2 = stream.ReadChar();
            //char c3 = stream.ReadChar();
            //char c4 = stream.ReadChar();
            //char c5 = stream.ReadChar();
            //char c6 = stream.ReadChar();
            //char c7 = stream.ReadChar();

            //char c1 = (char)reader.Read();
            //char c2 = (char)reader.Read();
            //char c3 = (char)reader.Read();
            //char c4 = (char)reader.Read();
            //char c5 = (char)reader.Read();
            //char c6 = (char)reader.Read();
            //char c7 = (char)reader.Read();

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
