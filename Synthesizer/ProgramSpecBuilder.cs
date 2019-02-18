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

    

        private static ProgramSpec BuildProgramSpecFromSpec(XElement componentSpecsXML)
        {
            var argsList = componentSpecsXML.Descendants(key_inputArgs).Descendants(key_args).Descendants(key_type).Select(x => new Arg(x.Value.Trim())).ToList();

            var programSpecsList = componentSpecsXML.Descendants(key_programSpec)
                .Select(x => new Dictionary<string, string>()
                {
                    {key_input, x.Descendants(key_input).FirstOrDefault().Value.Trim() },
                    {key_output, x.Descendants(key_output).FirstOrDefault().Value.Trim() }
                }).ToList();

            var examples = new List<Example>();

            foreach(var componentSpec in programSpecsList)
            {
                var inputSplitted = componentSpec[key_input].SplitBy("--");
                var inputs = new List<List<string>>()
                {
                    inputSplitted.First().Replace("[", String.Empty).Replace("]", String.Empty).SplitBy(",").Select(x => x.Trim()).ToList(),
                    new List<string>()
                    {
                        inputSplitted.Last()
                    }
                };
                var output = componentSpec[key_output].Replace("[", String.Empty).Replace("]", String.Empty).SplitBy(",").Select(x => x.Trim()).ToList();

                examples.Add(new Example(inputs, output, context));
                //programSpecs.Add(new List<List<string>>() {one, two });
            }

            return new ProgramSpec(examples, argsList);    
        }
    }
}
