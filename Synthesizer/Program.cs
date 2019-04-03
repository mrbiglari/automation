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
            var z3ComponentsSpecs = new List<Z3ComponentSpecs>();
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
                    currentNode = grammar.generateRandomAssignment(currentNode, lemmas, z3ComponentsSpecs, context, grammar);

                    var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SATEncode(z3ComponentsSpecs, context, programSpec, programRoot, grammar);

                    var unSATCore = SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);

                    if (unSATCore?.Count != 0)
                    {
                        unSATCores.Add(unSATCore);

                        //var lemma = new Lemma();
                        //foreach (var clause in unSATCore)
                        //{
                        //    var lemmaClause = new LemmaClause();
                        //    lemmaClause.Add(context.MkNot(clause.expression));
                        //    lemma.Add(lemmaClause);
                        //}
                        //lemmas.Add(lemma);

                        foreach (var clause in unSATCore)
                        {
                           // var lemma = new Lemma();
                            var rule = programRoot.GetAtIndex(Int32.Parse(clause.index)).rule;
                            var componentsToCheck = grammar.productions.Where(x => x.leftHandSide == rule.leftHandSide && x.rightHandSide.First() != clause.name)
                                .Select(x => x.rightHandSide.First()).ToList();

                            var lemmaClause = new LemmaClause();

                            var lemmaSub = unSATCore.Where(x => x != clause).Select(x =>
                            {
                                lemmaClause = new LemmaClause();
                                lemmaClause.Add(context.MkNot(x.expression));
                                return lemmaClause;
                            }).AsLemma();

                            lemmaClause = new LemmaClause();
                            lemmaClause.Add(context.MkNot(clause.expression));

                            if (clause.spec != null && z3ComponentsSpecs.Any(x => x.key == clause.name && x.type != ComponentType.Parameter))
                            {
                                foreach (var component in componentsToCheck)
                                {
                                    
                                    var componentSpec = z3ComponentsSpecs.Where(x => x.key == component).FirstOrDefault();
                                    if (componentSpec != null)
                                    {
                                        var z3ComponentSpec = context.MkAnd(ComponentSpecsBuilder.GetComponentSpec(componentSpec));

                                        var check = context.MkNot(context.MkImplies(z3ComponentSpec, clause.spec));
                                        var lightEncoding = unSATCore.Where(x => x != clause);
                                        if (SMTSolver.CheckIfUnSAT(context, check))
                                        {
                                            lemmaClause.Add
                                                (
                                                    context.MkNot
                                                    (
                                                        context.MkBoolConst($"C_{clause.index}_{component}")
                                                    )
                                                );
                                        }
                                    }
                                    
                                }
                            }
                            lemmaSub.Add(lemmaClause);
                            lemmaSub.lemmaLength = unSATCores.SelectMany( x => x).Max(x => x.index).ToInt();
                            lemmas.Add(lemmaSub);
                        }

                        if (currentNode.Parent != null)
                        {
                            //currentNode.Parent.Children.Remove(currentNode);
                            var index = currentNode.Parent.Children.IndexOf(currentNode);
                            currentNode = currentNode.Parent;
                            currentNode.holes.Push(currentNode.holesBackTrack.Pop());
                            currentNode.Children[index].MakeHole();

                            
                        }
                        else
                        {
                            programRoot = new TreeNode<string>();
                            currentNode = programRoot;
                        }
                    }

                    if (lemmas.IsUnSAT(context))
                        return;

                    if (programRoot.IsConcrete)
                    {
                        programRoot.Visualize();
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
            var z3ComponentsSpecs = new List<Z3ComponentSpecs>();
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