using System;
using System.Collections.Generic;
using System.Linq;
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
        public Random rand = new Random(5);
        public UnSatCores unSATCores;
        public Lemmas lemmas;


        public UnSatCore CheckConflict(List<Z3ComponentSpecs> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<string> programRoot, Grammar grammar)
        {
            var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SMTEncode(componentSpecs, context, programSpec, programRoot, grammar);

            return SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);
        }

        public Lemma AnalyzeConflict(UnSatCore unSATCore, List<Z3ComponentSpecs> z3ComponentsSpecs, Context context, TreeNode<string> programRoot, Grammar grammar)
        {
            var lemma = new Lemma();
            foreach (var clause in unSATCore)
            {
                var rule = programRoot.GetAtIndex(Int32.Parse(clause.index)).rule;
                var componentsToCheck = grammar.productions.Where(x => x.leftHandSide == rule.leftHandSide)
                    .Select(x => x.rightHandSide.First()).ToList();

                var lemmaClause = new LemmaClause();

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
                                lemmaClause.Add(context.MkNot(context.MkBoolConst($"C_{clause.index}_{component}")));
                            }
                        }
                    }
                }
                //lemma.lemmaLength = unSATCores.SelectMany(x => x).Max(x => x.index).ToInt();
                lemma.Add(lemmaClause);
            }
            return lemma;
        }

        public TreeNode<string> BackTrack(UnSatCore unSATCore, Grammar grammar, TreeNode<string> currentNode, TreeNode<string> programRoot)
        {
            int index = 0;
            while (unSATCore.First().index.ToInt() != currentNode.index)
            {
                if (!grammar.RuleResultsInLeaf(grammar, currentNode.rule))
                    grammar.productions.Add(currentNode.rule);
                currentNode = currentNode.Parent;
            }

            if (currentNode.Parent != null)
            {
                if (!grammar.RuleResultsInLeaf(grammar, currentNode.rule))
                    grammar.productions.Add(currentNode.rule);
                index = currentNode.Parent.Children.IndexOf(currentNode);
                currentNode = currentNode.Parent;

                //currentNode.holes.Push(grammar.productions.Where(x => x.component == currentNode.Children[index].Data).First().leftHandSide);
                currentNode.holes.Push(currentNode.holesBackTrack.Pop());
                currentNode.Children[index].MakeHole();
            }
            else
            {
                if (!grammar.RuleResultsInLeaf(grammar, currentNode.rule))
                    grammar.productions.Add(currentNode.rule);
                programRoot = new TreeNode<string>();
                currentNode = programRoot;
            }

            return programRoot;
        }

        public void Synthesize(int demand)
        {
            var z3ComponentsSpecs = new List<Z3ComponentSpecs>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                var typeSpecs = TypeSpecBuilder.Build(path_typeSpec, context);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context, typeSpecs);
                var grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs, rand);
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context, programSpec, grammar);

                var numberOfPrograms = 0;

                var programRoot = new TreeNode<string>();
                lemmas = new Lemmas();
                var currentNode = programRoot;
                while (true)
                {
                    currentNode = grammar.Decide(programRoot, lemmas, context, grammar);
                    programRoot.Visualize();
                    grammar.Propogate(programRoot, lemmas, context, grammar);

                    var unSATCore = CheckConflict(z3ComponentsSpecs, context, programSpec, programRoot, grammar);

                    if (unSATCore?.Count != 0)
                    {
                        var lemma = AnalyzeConflict(unSATCore, z3ComponentsSpecs, context, programRoot, grammar);
                        lemmas.Add(lemma);

                        programRoot = BackTrack(unSATCore, grammar, currentNode, programRoot);
                    }

                    if (lemmas.IsUnSAT(context))
                        return;

                    if (programRoot.IsConcrete)
                    {
                        Console.WriteLine("\nConcrete progam found:");
                        programRoot.Visualize();
                        Console.WriteLine("#######################################");
                        programRoot = new TreeNode<string>();
                        currentNode = programRoot;
                        lemmas.Clear();
                        //unSATCores.Clear();
                        grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs, rand);

                        if (numberOfPrograms + 1 == demand)
                            break;
                        else
                            numberOfPrograms++;
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            var program = new Program();
            while (true)
            {
                Console.Write("Please specify the amount of concrete programs:");
                var numberOfPrograms = Convert.ToInt32(Console.ReadLine());
                program.Synthesize(numberOfPrograms);
            }

        }
    }
}