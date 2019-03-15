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
        public const string path_typeSpec = specsFolderPath + "TypeSpec.xml";

        public void GeneratePartialPrograms()
        {
            var z3ComponentsSpecs = new List<Tuple<string, string>>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {

                var typeSpecs = TypeSpecBuilder.Build(path_typeSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context, typeSpecs);
                var grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs);
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context, programSpec, grammar);
                var lemmas = new Lemmas();

                var unSATCores = new UnSatCores();
                var programRoot = new TreeNode<string>();
                var currentNode = programRoot;
                while (true)
                {
                    currentNode = grammar.generateRandomAssignment(currentNode, unSATCores, z3ComponentsSpecs, context);

                    var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SATEncode(z3ComponentsSpecs, context, programSpec, programRoot, grammar);

                    var unSATCore = SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);

                    if (unSATCore?.Count != 0)
                    {
                        unSATCores.Add(unSATCore);

                        var lemma = new Lemma();
                        foreach (var clause in unSATCore)
                        {
                            var rule = programRoot.GetAtIndex(Int32.Parse(clause.index)).rule;
                            var componentsToCheck = grammar.productions.Where(x => x.leftHandSide == rule.leftHandSide && x.rightHandSide.First() != clause.name)
                                .Select(x => x.rightHandSide.First()).ToList();

                            var lemmaClause = new LemmaClause();
                            lemmaClause.Add
                                (
                                    context.MkNot
                                    (
                                        context.MkBoolConst($"C_{clause.index}_{clause.name}")
                                    )
                                );

                            foreach (var component in componentsToCheck)
                            {
                                var componentSpec = z3ComponentsSpecs.Where(x => x.Item1 == component).FirstOrDefault();
                                if (componentSpec != null)
                                {
                                    var z3ComponentSpec = context.MkAnd(ComponentSpecsBuilder.GetComponentSpec(componentSpec));

                                    var check = context.MkNot(context.MkImplies(z3ComponentSpec, clause.spec));

                                    if (SMTSolver.CheckIfUnSAT(context, check))
                                        lemmaClause.Add
                                            (
                                                context.MkNot
                                                (
                                                    context.MkBoolConst($"C_{clause.index}_{component}")
                                                )
                                            );
                                }
                            }
                            if (lemmaClause.Count > 0)
                                lemma.Add(lemmaClause);

                            //var s = grammar.productions.Where(x => x.rightHandSide.Contains(clause.name)).First();

                        }
                        lemmas.Add(lemma);

                        if (currentNode.Parent != null)
                        {
                            currentNode.Parent.Children.Remove(currentNode);
                            currentNode = currentNode.Parent;
                        }
                        else
                        {
                            programRoot = new TreeNode<string>();
                            currentNode = programRoot;
                        }
                    }

                    if (unSATCores.IsUnSAT(context))
                        return;

                    if (programRoot.IsConcrete)
                    {
                        programRoot = new TreeNode<string>();
                        currentNode = programRoot;
                        lemmas.Clear();
                        unSATCores.Clear();
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
                var typeSpecs = TypeSpecBuilder.Build(path_typeSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context, typeSpecs);
                var grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs);
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context, programSpec, grammar);
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