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
        public const string key_programSpecs= "ProgramSpecs";
        public const string key_programSpec = "Spec";
        public const string key_input = "Input";
        public const string key_output = "Output";
        public static Context context;

        public static List<ProgramSpec> Build(string fileName, Context ctx)
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

    

        private static List<ProgramSpec> BuildProgramSpecFromSpec(XElement componentSpecsXML)
        {
            var programSpecsList = componentSpecsXML.Descendants(key_programSpec)
                .Select(x => new Dictionary<string, string>()
                {
                    {key_input, x.Descendants(key_input).FirstOrDefault().Value.Trim() },
                    {key_output, x.Descendants(key_output).FirstOrDefault().Value.Trim() }
                }).ToList();

            var programSpecs = new List<ProgramSpec>();

            foreach(var componentSpec in programSpecsList)
            {
                var input = componentSpec[key_input].Replace("[",String.Empty).Replace("]", String.Empty).SplitBy(",").Select(x => x.Trim()).ToList();
                var output = componentSpec[key_output].Replace("[", String.Empty).Replace("]", String.Empty).SplitBy(",").Select(x => x.Trim()).ToList();

                programSpecs.Add(new ProgramSpec(input, output, context));
                //programSpecs.Add(new List<List<string>>() {one, two });
            }

            return programSpecs;    
        }
    }
}
