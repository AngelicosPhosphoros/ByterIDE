using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompilerLib;
using System.IO;
using System.Text.RegularExpressions;

namespace bytc
{
    class Program
    {
        static char[,] LoadSource(string filename)
        {
            StreamReader sr = File.OpenText(filename);
            char[,] res = new char[16, 16];
            for(int i =0;i<16;i++)
            {
                string s = sr.ReadLine();
                for (int j = 0; j < 16; j++) res[i, j] = s[j];
            }
            return res;
        }
        static Tuple<char[,], Dictionary<char, char[,]>> LoadFiles(string projectFileName)
        {
            try
            {

                StreamReader sr = File.OpenText(projectFileName);
                int moduleNumber = int.Parse(sr.ReadLine());
                moduleNumber--;
                string s;
                do
                {
                    s = sr.ReadLine();
                    s=s.Trim(' ');
                }
                while (!Regex.IsMatch(s, @"\[\w+\|(\(|\)|\w|\.)[\w\\\.\(\)\s]+\]")); // this is [methodName|path]
                s = s.Trim('[', ']');
                char[,] main = LoadSource(s.Split('|')[1]);
                Dictionary<char, char[,]> methods = new Dictionary<char, char[,]>();
                for (int i = 0; i < moduleNumber; i++)
                {
                    s = sr.ReadLine().Trim('[',']');
                    string [] str = s.Split('|');
                    char name = str[0][0];
                    if (methods.Keys.Count(n=>{return n==name;})>0 || str.Length>2) throw new Exception("");
                    char[,] source = LoadSource(str[1]);
                    methods.Add(name, source);
                }
                return new Tuple<char[,], Dictionary<char, char[,]>>(main, methods);
            }
            catch
            {
                throw new FileLoadException("Error in loading project.");
            }
        }
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error in bytc.exe. Calling convention: bytc sourcefile");
                return;
            }
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
                Tuple<char[,], Dictionary<char, char[,]>> sources = LoadFiles(args[0]);
                string exename = Path.GetFullPath(args[0]);
                exename = exename.Remove(exename.LastIndexOf('.')) + ".exe";
                Compiler comp = new Compiler(sources.Item1, sources.Item2);
                var result = comp.GenerateCode(exename);
                switch (result.Item1)
                {
                    case 0: Console.WriteLine("Compilation successful"); break;
                    case 1: Console.WriteLine("Compilation failed due errors in source code:");
                        foreach (var k in result.Item2.Criticals)
                            Console.WriteLine("{0}, in module {1} in {2}:{3}", k.Type, k.ModuleName, k.Row, k.Column);
                        break;
                    case 2: Console.WriteLine("Compilation failed."); break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
