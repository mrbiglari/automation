using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis
{
    public class ProgramSpec
    {
        public List<Example> examples;
        public List<Arg> args;
        public List<Parameter> parameters;

        public ProgramSpec(List<Example> examples, List<Arg> args)
        {
            this.examples = examples;
            this.args = args;
        }
    }

    public enum ArgType
    {
        Unknown,
        List,
        Int,
        Other
    }

    public enum ParameterType
    {
        Input,
        Output
    }

    public static class Symbols
    {
        public const string argTypeOpeningSymbol = "(";
        public const string argTypeClosingSymbol = ")";
        public const string argSeperator = "--";
        public const string seperator = ",";

        public const string listType = "list";
        public const string intType = "int";
        public const string otherType = "other";

        public static List<string> types = new List<string>()
        {
            listType,
            intType,
            otherType,
        };

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

    public class Parameter
    {
        public ParameterType parameterType;
        public ArgType argType;
        public object obj;
        public int index;

        public Parameter(ParameterType parameterType, ArgType argType, object obj, int index)
        {
            this.argType = argType;
            this.obj = obj;
            this.parameterType = parameterType;
            this.index = index;
        }

        public T As<T>()
        {
            return (T)obj;
        }
    }

    public class Example
    {
        public List<Parameter> parameters;

        public BoolExpr spec;
        public string specAsString;

        private const string first = Symbols.first;
        private const string last = Symbols.last;
        private const string size = Symbols.size;
        private const string max = Symbols.max;
        private const string min = Symbols.min;

        private string Formulate_Int(string arg_1, ParameterType param, int index)
        {
            var indexAsString = (index > 0) ? index.ToString() : String.Empty;
            if (param == ParameterType.Input)
                return Symbols.inputArg + indexAsString + RelationalOperators.operators[ERelationalOperators.Eq] + arg_1;
            else if (param == ParameterType.Output)
                return Symbols.outputArg + indexAsString + RelationalOperators.operators[ERelationalOperators.Eq] + arg_1;
            return null;
        }
        private string Formulate_List(string arg_1, string arg_2, ParameterType param, int index)
        {
            var indexAsString = (index > 0) ? index.ToString() : String.Empty;
            if (param == ParameterType.Input)
                return Symbols.inputArg + indexAsString + Symbols.dot + arg_1 + RelationalOperators.operators[ERelationalOperators.Eq] + arg_2;
            else if (param == ParameterType.Output)
                return Symbols.outputArg + indexAsString + Symbols.dot + arg_1 + RelationalOperators.operators[ERelationalOperators.Eq] + arg_2;

            return String.Empty;
        }

        public Example(List<Parameter> parameters, List<Tuple<string, List<string>>> argSpecList, Context context)
        {
            this.parameters = new List<Parameter>();

            var argSpec = argSpecList.Where(x => x.Item1 == ArgType.List.ToString().ToLower()).First();

            foreach (var parameter in parameters)
            {
                switch (parameter.argType)
                {
                    case (ArgType.List):
                        var inputAsList = parameter.As<List<string>>();
                        var list = new List<string>();
                        foreach (var property in argSpec.Item2)
                        {
                            switch (property)
                            {
                                case (first):
                                    list.Add(Formulate_List(property, inputAsList.First(), parameter.parameterType, parameter.index));
                                    break;

                                case (last):
                                    list.Add(Formulate_List(property, inputAsList.Last(), parameter.parameterType, parameter.index));
                                    break;

                                case (max):
                                    list.Add(Formulate_List(property, inputAsList.Select(x => Int32.Parse(x)).Max().ToString(), parameter.parameterType, parameter.index));
                                    break;

                                case (min):
                                    list.Add(Formulate_List(property, inputAsList.Select(x => Int32.Parse(x)).Min().ToString(), parameter.parameterType, parameter.index));
                                    break;

                                case (size):
                                    list.Add(Formulate_List(property, inputAsList.Count().ToString(), parameter.parameterType, parameter.index));
                                    break;
                            }
                        }
                        //parameter.obj = list;
                        this.parameters.Add(new Parameter(parameter.parameterType, parameter.argType, list, parameter.index));
                        break;

                    case (ArgType.Int):
                        this.parameters.Add(new Parameter(parameter.parameterType, parameter.argType, Formulate_Int(parameter.As<string>(), parameter.parameterType, parameter.index), parameter.index));
                        break;
                }
            }            
        }
    }
}
