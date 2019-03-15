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

    public class TypeSpec
    {
        public ArgType type;
        public List<string> properties;

        public TypeSpec(ArgType typeName, List<string> properties)
        {
            this.type = typeName;
            this.properties = properties;
        }
    }
    public class TypeSpecBuilder
    {
        public const string key_types = "Types";
        public const string key_type = "Type";
        public const string key_typeName = "TypeName";        
        public const string key_properties = "Properties";
        public static Context context;

        public static List<TypeSpec> Build(string fileName, Context ctx)
        {
            context = ctx;
            var specContent = GetTypeSpecsFile(fileName);
            return BuildTypeSpecFromSpec(specContent);
        }

        private static XElement GetTypeSpecsFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var componentSpecsFilepath = Path.Combine(currentDirectory, fileName);
            return XElement.Load(componentSpecsFilepath);
        }

        
        private static List<TypeSpec> BuildTypeSpecFromSpec(XElement componentSpecsXML)
        {
            var typeSpecList = componentSpecsXML.Descendants(key_type)
                .Select(x =>
                {
                    return new TypeSpec(
                           EnumHelper.ToEnum<ArgType>(x.Descendants(key_typeName).FirstOrDefault().Value.Trim()),
                           x.Descendants(key_properties).FirstOrDefault()?.Value.Trim().SplitBy(",") ?? null
                           );
                }    ).ToList();
            
            return typeSpecList;
        }
    }
}
