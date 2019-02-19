using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernateDemoApp
{
    public class ProgramSpec
    {
        public List<Example> examples;
        public List<Arg> args;

        public ProgramSpec(List<Example> examples, List<Arg> args)
        {
            this.examples = examples;
            this.args = args;
        }
    }

    public static class Symbols
    {
        public const string listType = "list";
        public const string intType = "int";
        public const string otherType = "other";

        public const string first = "first";
        public const string last = "last";
        public const string size = "size";
        public const string max = "max";
        public const string min = "min";
        public const string eq = "=";
        public const string inputArg = "x";
        public const string outputArg = "y";
        public const string dot = ".";

        public static List<string> properties = new List<string>()
        {
            first,
            last,
            size,
            max,
            min
        };
    }

    public class Example
    {
        public List<List<string>> inputs;
        public List<string> output;
        public BoolExpr spec;
        public string specAsString;
        //public List<string> specStringList = new List<string>();
        public Dictionary< string ,List<string>> specStringList = new Dictionary<string, List<string>>()
        {
            { Symbols.inputArg, new List<string>() },
            { Symbols.outputArg, new List<string>() }
        };

        private const string first = Symbols.first;
        private const string last = Symbols.last;
        private const string size = Symbols.size;
        private const string max = Symbols.max;
        private const string min = Symbols.min;
        private const string eq = Symbols.eq;
        private const string x = Symbols.inputArg;
        private const string y = Symbols.outputArg;
        private const string dot = Symbols.dot;

        

        private string Formulate(string arg_1, string arg_2, string param)
        {
            return param + dot + arg_1 + eq + arg_2;
        }

        public Example(List<List<string>> inputs, List<string> output, Context context)
        {
            this.inputs = inputs;
            this.output = output;
            foreach(var property in Symbols.properties)
            {
                switch(property)
                {
                    case (first):
                        specStringList[Symbols.inputArg].Add(Formulate(first, inputs.First().First(), x));
                        specStringList[Symbols.outputArg].Add(Formulate(first, output.First(), y));
                        break;

                    case (last):
                        specStringList[Symbols.inputArg].Add(Formulate(last, inputs.First().Last(), x));
                        specStringList[Symbols.outputArg].Add(Formulate(last, output.Last(), y));
                        break;

                    case (max):
                        specStringList[Symbols.inputArg].Add(Formulate(max, inputs.First().Select(x => Int32.Parse(x)).Max().ToString(), x));
                        specStringList[Symbols.outputArg].Add(Formulate(max, output.Select(x => Int32.Parse(x)).Max().ToString(), y));
                        break;

                    case (min):
                        specStringList[Symbols.inputArg].Add(Formulate(min, inputs.First().Select(x => Int32.Parse(x)).Min().ToString(), x));
                        specStringList[Symbols.outputArg].Add(Formulate(min, output.Select(x => Int32.Parse(x)).Min().ToString(), y));
                        break;

                    case (size):
                        specStringList[Symbols.inputArg].Add(Formulate(size, inputs.First().Count.ToString(), x));
                        specStringList[Symbols.outputArg].Add(Formulate(size, output.Count.ToString(), y));
                        break;
                }
            }            
        }
    }
}
