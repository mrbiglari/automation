using CSharpTree;
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

        public static void WriteBenchmark(TreeNode<string> root, List<List<object>> programSpec)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(asm.Location);


            

            var folderPath = $"{path}\\MyFolder";
            System.IO.Directory.CreateDirectory(folderPath);

            DirectoryInfo di = new DirectoryInfo(folderPath);
            var files = di.GetFiles("ProgramSpec*");


            var xElement_programSpec = new XElement(Resources.key_programSpec);

            var xElement_programDefinition = new XElement(Resources.key_programDefinition);
            xElement_programDefinition.Value = $"list(x1), int(x2) -- list(y)";
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
            xElement_programSpec.Save($"{folderPath}\\ProgramSpec{files.Count() + 1}.xml");
        }

        public static void CreateBenchmark(Random random)
        {            
            var context = new Microsoft.Z3.Context(new Dictionary<string, string>() { { "proof", "true" } });

            var typeSpecs = TypeSpecBuilder.Build(Resources.path_typeSpec);
            var grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);
            var root = new TreeNode<string>();
            var programSpecs = new List<List<List<object>>>();
            while (true)
            {
                grammar.Decide(root, new Lemmas(), context, grammar);
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
                                var result = CreateRandomParamsAndExecuteProgram(root, random);
                                programSpec.Add(result);
                                if (programSpec.Count == 5)
                                {
                                    programSpecs.Add(programSpec);
                                    WriteBenchmark(root, programSpec);
                                    root = new TreeNode<string>();
                                    grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);
                                    break;
                                }

                            }
                        }
                        catch (Exception exception)
                        {
                            ;
                            root = new TreeNode<string>();
                            grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);
                        }

                    }
                    else
                    {
                        root = new TreeNode<string>();
                        grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);
                    }

                    if (programSpecs.Count == 50)
                        break;
                }
            }
        }

        public static List<object> CreateRandomParamsAndExecuteProgram(TreeNode<string> root, Random random)
        {
            var runner = new ProgramRunner();
            var programExample = new List<object>();

            var value_limit = 100;
            var arg_1 = random.InstantiateRandomly_List_Of_Int(value_limit);
            var arg_2 = random.InstantiateRandomly_Int(value_limit);
            programExample.AddRange(new List<object> { arg_1, arg_2 });
            var result = runner.ExecuteProgram(root, programExample.ToArray());
            programExample.Add(result);
            return programExample;
        }

        public static void ImplementProgram()
        {
            var methodName = "last";
            var type = typeof(ComponentConcreteSpecs);
            //var method = type.GetMethod(methodName, BindingFlags.Public);
            var method = type.GetMethod(methodName);
            var args = method.GetGenericArguments();
            var result = method.Invoke(null, new object[] { new List<int> { 1, 2, 3 } });
        }
    }
}
