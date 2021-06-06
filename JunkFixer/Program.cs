using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;


namespace JunkFixer
{
    public class Program
    {   
        static void Main(string[] args)
        {
            Console.Title = "JunkRemover";
            if (args.Length == 1)
            {
                ModuleDefMD Module = ModuleDefMD.Load(args[0]);
                Log($"{Module.Name} Loaded Successfly", LogType.Done);
                new Junk(Module);
            }
            else
            {
                Log("Please Drag & Drop your target exe...", LogType.Error);
                Console.ReadKey();
                return;
            }


        }

        public enum LogType
        {
            Done,
            Error
        }
        public static void Log(string m, LogType Type)
        {
            switch (Type)
            {
                case LogType.Done:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Done");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("]");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" {0}", m);
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("Error");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("]");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" {0}", m);
                    break;
            }
        }
    }
}
