using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using CSharpTree;
using Microsoft.Z3;
namespace Synthesis
{
    public class Program
    {
        public const string specsFolderPath = "Specs/";
        public const string path_grammarSpec = specsFolderPath + "GrammarSpec.xml";
        public const string path_componentSpec = specsFolderPath + "ComponentSpecs.xml";
        public const string path_programSpec = specsFolderPath + "ProgramSpec.xml";

        public void GeneratePartialPrograms()
        {
            var z3ComponentsSpecs = new List<Tuple<string, string>>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context);
                var grammar = GrammarBuilder.Build(path_grammarSpec);


                var lemmas = new Lemmas();
                var programRoot = new TreeNode<string>();
                var currentNode = programRoot;
                while (true)
                {
                    currentNode = grammar.generateRandomAssignment(currentNode, lemmas, z3ComponentsSpecs, context);

                    var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SATEncode(z3ComponentsSpecs, context, programSpec, programRoot, grammar);

                    var lemma = SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);

                    if (lemma.Count != 0)
                        lemmas.Add(lemma);
                    else
                    {
                        currentNode.Parent.Children.Remove(currentNode);
                        currentNode = currentNode.Parent;
                    }

                    if (lemmas.IsUnSAT(context))
                        return;

                    if (programRoot.IsConcrete)
                    {
                        programRoot = new TreeNode<string>();
                        currentNode = programRoot;
                    }

                    programRoot.Visualize();
                }
            }
        }
        public void GenerateConcretePrograms()
        {
            var z3ComponentsSpecs = new List<Tuple<string, string>>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context);
                var grammar = GrammarBuilder.Build(path_grammarSpec);

                var counter = 1;
                while (true)
                {
                    if (counter % 100 == 0)
                        Console.ReadLine();
                    var program = grammar.generateRandomProgram();
                    program = program.ChipRoot();
                    var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SATEncode(z3ComponentsSpecs, context, programSpec, program, grammar);

                    var unsatCore = SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);
                    program.Visualize();

                    counter++;
                }
            }
        }

        static void Main(string[] args)
        {

            var program = new Program();
            //program.GenerateConcretePrograms();
            program.GeneratePartialPrograms();
        }
    }
}