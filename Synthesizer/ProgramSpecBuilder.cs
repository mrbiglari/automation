using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NHibernateDemoApp
{
    public class ProgramSpecBuilder
    {
        public const string key_args = "Arg";
        public const string key_type = "Type";
        public const string key_properties = "Properties";
        public const string key_inputArgs = "InputArgs";
        public const string key_outputArgs = "OutputArgs";
        public const string key_programSpec = "Example";
        public const string key_input = "Input";
        public const string key_output = "Output";
        public static Context context;

        public static ProgramSpec Build(string fileName, Context ctx)
        {
            context = ctx;
            var specContent = GetProgramSpecsFile(fileName);
            return BuildProgramSpecFromSpec(specContent);
        }

        private static XElement GetProgramSpecsFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var componentSpecsFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(componentSpecsFilepath);
        }

        public static Dictionary<ParameterType, Dictionary<ArgType, List<Parameter>>> parameterList;
        public static List<Parameter> parameterL;

        private static void InitializeParametersList()
        {
            parameterList = new Dictionary<ParameterType, Dictionary<ArgType, List<Parameter>>>();

            Enum.GetValues(typeof(ParameterType)).Cast<int>().ToList().ForEach((y) =>
            {
                parameterList.Add((ParameterType)y, new Dictionary<ArgType, List<Parameter>>());
                Enum.GetValues(typeof(ArgType)).Cast<int>().ToList().ForEach((x) =>
                    {
                        parameterList[(ParameterType)y].Add((ArgType)x, new List<Parameter>());
                    });
            });
        }

        private static ProgramSpec BuildProgramSpecFromSpec(XElement componentSpecsXML)
        {            
            //InitializeParametersList();

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
                parameterL = new List<Parameter>();
                var inputSplittedList = componentSpec[ParameterType.Input].SplitBy(Symbols.argSeperator).Select((x, index) => Tuple.Create(index + 1, x)).ToList();
                UpdateParametersList(inputSplittedList, ParameterType.Input);

                var outputSplittedList = componentSpec[ParameterType.Output].SplitBy(Symbols.argSeperator).Select((x, index) => Tuple.Create(index + 1, x)).ToList();
                UpdateParametersList(outputSplittedList, ParameterType.Output);

                examples.Add(new Example(parameterL, argSpecList, context));
            }

            return new ProgramSpec(examples, argTypesList);
        }


        public static void UpdateParametersList(List<Tuple<int, string>> parameters, ParameterType parameterType)
        {
            foreach (var parameter in parameters)
            {
                var argTypeAsString = parameter.Item2.SplitBy(Symbols.argTypeOpeningSymbol).First();
                var argValueAsString = parameter.Item2.SplitBy(Symbols.argTypeOpeningSymbol).Last().Replace(Symbols.argTypeClosingSymbol, String.Empty);

                var type = EnumHelper.GetEnumValue<ArgType>(argTypeAsString);
                var index = (parameters.Count() > 1) ? parameter.Item1 : 0;
                switch (type)
                {
                    case ArgType.List:
                        var arg = argValueAsString.SplitBy(Symbols.seperator).Select(x => x.Trim()).ToList();
                        
                        parameterL.Add(new Parameter(parameterType, type, arg, index));
                        break;
                    case ArgType.Int:
                        parameterL.Add(new Parameter(parameterType, type, argValueAsString, index));
                        break;
                }
            }
        }
    }
}
