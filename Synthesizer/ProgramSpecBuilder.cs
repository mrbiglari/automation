using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Synthesis
{
    public class ProgramSpecBuilder
    {
        public const string key_args = "Arg";
        public const string key_programDefinition = "ProgramDefinition";
        public const string key_type = "Type";
        public const string key_properties = "Properties";
        public const string key_inputArgs = "InputArgs";
        public const string key_outputArgs = "OutputArgs";
        public const string key_programSpec = "Example";
        public const string key_input = "Input";
        public const string key_output = "Output";
        public static Context context;

        private const string first = Symbols.first;
        private const string last = Symbols.last;
        private const string size = Symbols.size;
        private const string max = Symbols.max;
        private const string min = Symbols.min;

        public static ProgramSpec Build(string fileName, Context ctx, List<TypeSpec> typeSpecs)
        {
            context = ctx;
            var specContent = GetProgramSpecsFile(fileName);
            return BuildProgramSpecFromSpec(specContent, typeSpecs);
        }

        private static XElement GetProgramSpecsFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var componentSpecsFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(componentSpecsFilepath);
        }

        public static List<Parameter> parameterList;

        private static string Formulate_Int(string arg_1, ParameterType param, int index)
        {
            var indexAsString = (index > 0) ? index.ToString() : String.Empty;
            if (param == ParameterType.Input)
                return Symbols.inputArg + indexAsString + RelationalOperators.operators[ERelationalOperators.Eq] + arg_1;
            else if (param == ParameterType.Output)
                return Symbols.outputArg + indexAsString + RelationalOperators.operators[ERelationalOperators.Eq] + arg_1;
            else if (param == ParameterType.Other)
                return Symbols.outputArg + RelationalOperators.operators[ERelationalOperators.Eq] + arg_1;
            return null;
        }
        private static string Formulate_List(string arg_1, string arg_2, ParameterType param, int index)
        {
            var indexAsString = (index > 0) ? index.ToString() : String.Empty;
            if (param == ParameterType.Input)
                return Symbols.inputArg + indexAsString + Symbols.dot + arg_1 + RelationalOperators.operators[ERelationalOperators.Eq] + arg_2;
            else if (param == ParameterType.Output)
                return Symbols.outputArg + indexAsString + Symbols.dot + arg_1 + RelationalOperators.operators[ERelationalOperators.Eq] + arg_2;
            else if (param == ParameterType.Other)
                return Symbols.outputArg + Symbols.dot + arg_1 + RelationalOperators.operators[ERelationalOperators.Eq] + arg_2;

            return String.Empty;
        }

        public static Parameter GetParamByType(Parameter parameter, TypeSpec argSpec)
        {
            switch (parameter.argType)
            {
                case (ArgType.List):
                    var inputAsList = parameter.obj.ToString().SplitBy(Symbols.cotation).Select(x => x.Trim()).ToList();
                    var list = new List<string>();
                    foreach (var property in argSpec.properties)
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
                    return new Parameter(parameter.parameterType, parameter.argType, list, parameter.index);

                case (ArgType.Int):
                    return new Parameter(parameter.parameterType, parameter.argType, Formulate_Int(parameter.As<string>(), parameter.parameterType, parameter.index), parameter.index);
            }
            return null;
        }

        private static ProgramSpec BuildProgramSpecFromSpec(XElement componentSpecsXML, List<TypeSpec> typeSpecs)
        {
            var programDefinition = componentSpecsXML.Descendants(key_programDefinition).First().Value.Trim();
            
            var inputParametersSplittedList = programDefinition.SplitBy(Symbols.argSeperator).First().SplitBy(Symbols.seperator)
                .Select(
                    (x, index) =>
                    new Parameter(ParameterType.Input, EnumHelper.ToEnum<ArgType>(x.SplitBy("(").First()), x.SplitBy("(").Last().Replace(")", ""), index + 1)                    
                ).ToList();
            //UpdateParametersList(inputSplittedList, ParameterType.Input);

            var outputParametersSplittedList = programDefinition.SplitBy(Symbols.argSeperator).Last().SplitBy(Symbols.seperator)
                .Select(
                    (x, index) =>
                    new Parameter(ParameterType.Output, EnumHelper.ToEnum<ArgType>(x.SplitBy("(").First()), x.SplitBy("(").Last().Replace(")", ""), index + 1)
                ).ToList();

            var parameters = inputParametersSplittedList.Union(outputParametersSplittedList).ToList();
            //UpdateParametersList(outputSplittedList, ParameterType.Output);


            var argTypesList = componentSpecsXML.Descendants(key_inputArgs).Descendants(key_args).Descendants(key_type).Select(x => new Arg(x.Value.Trim())).ToList();

            var argSpecList = componentSpecsXML.Descendants(key_inputArgs).Descendants(key_args)
                .Select(x =>
                    Tuple.Create
                    (
                        x.Descendants(key_type).First().Value.Trim(),
                        (x.Descendants(key_properties).FirstOrDefault()?.Value.Trim() ?? String.Empty).SplitBy(Symbols.seperator))
                    ).ToList();


            var programSpecsList = componentSpecsXML.Descendants(key_programSpec)
                .Select(x => new Dictionary<ParameterType, string>()
                {
                    {ParameterType.Input, x.Descendants(key_input).FirstOrDefault().Value.Trim() },
                    {ParameterType.Output, x.Descendants(key_output).FirstOrDefault().Value.Trim() }
                }).ToList();

            var examples = new List<Example>();

            foreach (var componentSpec in programSpecsList)
            {
                parameterList = new List<Parameter>();
                var inputSplittedList = componentSpec[ParameterType.Input].SplitBy(Symbols.argSeperator).Select((x, index) => Tuple.Create(index + 1, x)).ToList();
                UpdateParametersList(inputSplittedList, ParameterType.Input);

                var outputSplittedList = componentSpec[ParameterType.Output].SplitBy(Symbols.argSeperator).Select((x, index) => Tuple.Create(index + 1, x)).ToList();
                UpdateParametersList(outputSplittedList, ParameterType.Output);

                examples.Add(new Example(parameterList, typeSpecs, context));
            }

            return new ProgramSpec(examples, argTypesList, parameters);
        }

        public static void UpdateParametersList(List<Tuple<int, string>> parameters, ParameterType parameterType)
        {
            foreach (var parameter in parameters)
            {
                var argTypeAsString = parameter.Item2.SplitBy(Symbols.argTypeOpeningSymbol).First();
                var argValueAsString = parameter.Item2.SplitBy(Symbols.argTypeOpeningSymbol).Last().Replace(Symbols.argTypeClosingSymbol, String.Empty);

                var type = EnumHelper.GetEnumValue<ArgType>(argTypeAsString);
                var index = (parameters.Count() > 1) ? parameter.Item1 : 0;

                parameterList.Add(new Parameter(parameterType, type, argValueAsString, index));
            }
        }
    }
}
