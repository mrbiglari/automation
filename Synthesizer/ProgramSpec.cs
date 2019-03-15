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

        public ProgramSpec(List<Example> examples, List<Arg> args, List<Parameter> parameters)
        {
            this.examples = examples;
            this.args = args;
            this.parameters = parameters;
        }
    }

    public enum ArgType
    {
        Unknown = 0,
        List,
        Int,
        Other
    }

    public enum ParameterType
    {
        Unknown = 0,
        Input,
        Output,
        Other
    }

    public static class Symbols
    {
        public const string argTypeOpeningSymbol = "(";
        public const string argTypeClosingSymbol = ")";
        public const string argSeperator = "--";
        public const string seperator = ",";
        public const string blank = " ";
        public const string cotation = "'";

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
       
        public Example(List<Parameter> parameters, List<TypeSpec> argSpecList, Context context)
        {
            this.parameters = new List<Parameter>();

            var argSpec = argSpecList.Where(x => x.type == ArgType.List).First();

            foreach (var parameter in parameters)
            {
                var param = ProgramSpecBuilder.GetParamByType(parameter, argSpec);

                this.parameters.Add(param);
            }            
        }
    }
}
