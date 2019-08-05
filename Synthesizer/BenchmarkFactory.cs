using CSharpTree;
using Microsoft.Z3;
using Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Synthesizer
{
    public static class BenchmarkFactory
    {

        public static string Input(object x)
        {
            var type = x.GetType();
            var ret = String.Empty;
            if (type == typeof(List<int>))
                ret = $"list({String.Join(Symbols.cotation, (List<int>)x)})";
            else if (type == typeof(int))
                ret = $"int({x.ToString()})";
            return ret;
        }
        public static string Output(object x)
        {
            var type = x.GetType();
            var ret = String.Empty;
            if (type == typeof(List<int>))
                ret = $"list({String.Join(Symbols.cotation, (List<int>)x)})";
            else if (type == typeof(int))
                ret = $"list({x.ToString()})";
            return ret;
        }

        public static string TypedParam(Parameter parameter)
        {
            var paramAsString = parameter.obj.ToString();
            switch (parameter.argType)
            {                
                case (ArgType.List):
                    return $"list({paramAsString})";
                case (ArgType.Int):
                    return $"int({paramAsString})";
                default:
                    return String.Empty;
            }
        }

        public static void WriteBenchmark(TreeNode<string> root, List<List<object>> programSpec, List<Parameter> parameters, Context context)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(asm.Location);

            var folderPath = $"{path}\\MyFolder";
            System.IO.Directory.CreateDirectory(folderPath);

            DirectoryInfo di = new DirectoryInfo(folderPath);
            var files = di.GetFiles("ProgramSpec*");


            var xElement_programSpec = new XElement(Resources.key_programSpec);

            var xElement_programDefinition = new XElement(Resources.key_programDefinition);

            xElement_programDefinition.Value = $@"{String.Join(",",parameters
                .Where(x => x.parameterType == ParameterType.Input)
                .Select((x) => TypedParam(x)))} -- {parameters.Where(x => x.parameterType == ParameterType.Output).Select(x => TypedParam(x)).First()}";

            xElement_programSpec.Add(xElement_programDefinition);

            foreach(var example in programSpec)
            {
                var xElement_example = new XElement(Resources.key_example);

                var xElement_input = new XElement(Resources.key_input);
                var args_input = (List<object>)example.GetRange(0, example.Count - 1);
                var temp = args_input.Select(x => Input(x)).ToList();
                var args_input_AsString = String.Join(Symbols.argSeperator, temp);
                xElement_input.Value = args_input_AsString;

                var xElement_output = new XElement(Resources.key_output);
                var args_output_AsString = Output(example.Last());
                xElement_output.Value = args_output_AsString;

                xElement_example.Add(new object[] {xElement_input, xElement_output });
                xElement_programSpec.Add(xElement_example);
            }

            //var xElement_program = new XElement(Resources.key_program);
            //xElement_program.Value = root.Visualize

            var xElement_program = new XElement("Program");
            xElement_program.Add(Program.SAT_Encode(root, context));
            xElement_programSpec.Add(xElement_program);

            xElement_programSpec.Save($"{folderPath}\\ProgramSpec{files.Count() + 1}.xml");
        }

        public static void CreateBenchmark(Random random)
        {            
            var context = new Microsoft.Z3.Context(new Dictionary<string, string>() { { "proof", "true" } });

            var typeSpecs = TypeSpecBuilder.Build(Resources.path_typeSpec);
            var grammar = default(Grammar);
            var root = default(TreeNode<string>);
            var input_arg_limit = 5;
            var parameters = new List<Parameter>();
            SetupNewProgram(ref root, ref grammar, typeSpecs, random, input_arg_limit, ref parameters);

            var programSpecs = new List<List<List<object>>>();
            while (true)
            {
                grammar.Decide(root, new Lemmas(), context, grammar, null);
                if (root.IsConcrete)
                {
                    root.Visualize();
                    var componentNodes = root.Where(x => !x.IsLeaf).ToList();

                    if (componentNodes.Count > 5)
                    {
                        var programSpec = new List<List<object>>();

                        try
                        {
                            for (int i = 0; i < 500; i++)
                            {
                                var result = CreateRandomParamsAndExecuteProgram(root, random, parameters);
                                programSpec.Add(result);
                                if (programSpec.Count == 50)
                                {
                                    programSpecs.Add(programSpec);
                                    WriteBenchmark(root, programSpec, parameters, context);

                                    SetupNewProgram(ref root, ref grammar, typeSpecs, random, input_arg_limit, ref parameters);

                                    break;
                                }

                            }
                        }
                        catch (Exception exception)
                        {
                            SetupNewProgram(ref root, ref grammar, typeSpecs, random, input_arg_limit, ref parameters);
                        }

                    }
                    else
                    {
                        SetupNewProgram(ref root, ref grammar, typeSpecs, random, input_arg_limit, ref parameters);
                    }

                   
                }
                if (programSpecs.Count == 100)
                    break;
            }
        }

        public static void SetupNewProgram(ref TreeNode<string> root, ref Grammar grammar, List<TypeSpec> typeSpecs, Random random, int input_arg_limit, ref List<Parameter> parameters)
        {

            parameters = new List<Parameter>();

            for (int i = 1; i <= input_arg_limit; i++)
            {
                var inputParameter = new Parameter()
                {
                    index = i,
                    parameterType = ParameterType.Input,
                    argType = random.EnumValue<ArgType>(),
                    obj = $"x{i}"
                };
                parameters.Add(inputParameter);
            }

            var outputParameter = new Parameter()
            {
                index = 0,
                parameterType = ParameterType.Output,
                argType = ArgType.List,
                obj = $"y"
            };
            parameters.Add(outputParameter);

            root = new TreeNode<string>();
            grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, parameters);
        }

        public static List<object> CreateRandomParamsAndExecuteProgram(TreeNode<string> root, Random random, List<Parameter> parameters)
        {
            var runner = new ProgramRunner();
            var programExample = new List<object>();

            var value_limit = 100;

            var args = new List<object>();
            (parameters.Where(x => x.parameterType == ParameterType.Input).Count()).Times((i) =>
            {
                var arg = default(object);
                if(parameters[i].argType == ArgType.List)
                    arg = random.InstantiateRandomly_List_Of_Int(value_limit);
                else if (parameters[i].argType == ArgType.Int)
                    arg = random.InstantiateRandomly_Int(value_limit);
                args.Add(arg);
            });
            programExample.AddRange(args);
            var result = runner.ExecuteProgram(root, args.ToArray());
            programExample.Add(result);
            return programExample;
        }
    }
}
