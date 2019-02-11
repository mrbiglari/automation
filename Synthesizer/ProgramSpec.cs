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
        public List<string> input;
        public List<string> output;
        public BoolExpr spec;
        public string specAsString;
        public List<string> specSringList = new List<string>();

        private const string first = "first";
        private const string last = "last";
        private const string size = "size";
        private const string max = "max";
        private const string min = "min";
        private const string eq = "=";
        private const string x = "x";
        private const string y = "y";
        private const string dot = ".";

        private List<string> properties = new List<string>()
        {
            first,
            last,
            size,
            max,
            min
        };

        private string Formulate(string arg_1, string arg_2, string param)
        {
            return param + dot + arg_1 + eq + arg_2;
        }

        public ProgramSpec(List<string> input, List<string> output, Context context)
        {
            this.input = input;
            this.output = output;
            foreach(var property in properties)
            {
                switch(property)
                {
                    case (first):
                        specSringList.Add(Formulate(first, input.First(), x));
                        specSringList.Add(Formulate(first, output.First(), y));
                        break;

                    case (last):
                        specSringList.Add(Formulate(last, input.Last(), x));
                        specSringList.Add(Formulate(last, output.Last(), y));
                        break;

                    case (max):
                        specSringList.Add(Formulate(max, input.Select(x => Int32.Parse(x)).Max().ToString(), x));
                        specSringList.Add(Formulate(max, output.Select(x => Int32.Parse(x)).Max().ToString(), y));
                        break;

                    case (min):
                        specSringList.Add(Formulate(min, input.Select(x => Int32.Parse(x)).Min().ToString(), x));
                        specSringList.Add(Formulate(min, output.Select(x => Int32.Parse(x)).Min().ToString(), y));
                        break;

                    case (size):
                        specSringList.Add(Formulate(size, input.Count.ToString(), x));
                        specSringList.Add(Formulate(size, output.Count.ToString(), y));
                        break;
                }
            }
            
        }
    }
}
