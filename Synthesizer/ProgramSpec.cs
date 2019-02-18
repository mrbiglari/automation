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
        public List<string> specSringList = new List<string>();

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
                        specSringList.Add(Formulate(first, inputs.First().First(), x));
                        specSringList.Add(Formulate(first, output.First(), y));
                        break;

                    case (last):
                        specSringList.Add(Formulate(last, inputs.First().Last(), x));
                        specSringList.Add(Formulate(last, output.Last(), y));
                        break;

                    case (max):
                        specSringList.Add(Formulate(max, inputs.First().Select(x => Int32.Parse(x)).Max().ToString(), x));
                        specSringList.Add(Formulate(max, output.Select(x => Int32.Parse(x)).Max().ToString(), y));
                        break;

                    case (min):
                        specSringList.Add(Formulate(min, inputs.First().Select(x => Int32.Parse(x)).Min().ToString(), x));
                        specSringList.Add(Formulate(min, output.Select(x => Int32.Parse(x)).Min().ToString(), y));
                        break;

                    case (size):
                        specSringList.Add(Formulate(size, inputs.First().Count.ToString(), x));
                        specSringList.Add(Formulate(size, output.Count.ToString(), y));
                        break;
                }
            }            
        }
    }
}
