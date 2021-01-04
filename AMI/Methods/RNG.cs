using System;
using System.IO;
using AMYPrototype;
using Neitsillia.Methods;

namespace Neitsillia.Methods
{
    public partial class RNG
    {
        public int Next(int min, int max)
        {
            int rng = Program.rng.Next(min, max);
            PrintRNGResults(rng, min, max);
            return rng;
        }
        private void PrintRNGResults(int num, int min, int max, bool bread = false)
        {
            if (!File.Exists(@".\logs"))
                Directory.CreateDirectory(@".\logs");
            string filecontent = null;
            if (File.Exists(@".\logs\RNG.log") && bread)
            {
                StreamReader read = new StreamReader(@".\logs\RNG.log");
                int lines = 0;
                while (!read.EndOfStream)
                {
                    filecontent += read.ReadLine() + Environment.NewLine;
                    lines++;
                }
                if (lines > 5000)
                {
                    filecontent = null;
                    File.Delete(@".\logs\RNG.log");
                }
                read.Close();
            }

            StreamWriter results = new StreamWriter(@".\logs\RNG.log", !bread);
            results.WriteLine(DateTime.Now + " --- (" + min + ", "+ max +")  :  " + num);
            results.WriteLine(filecontent);
            results.Close();
            
        }
        public static string GenerateKey(int length = 10)
        {
            Random rng = new Random();
            string key = null;
            for (int i = 0; i < length; i++)
                key += (char)rng.Next(48, 127);
            return key;
        }
    }
}