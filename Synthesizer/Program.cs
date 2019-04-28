using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CSharpTree;
using Microsoft.Z3;
using Synthesizer;

namespace Synthesis
{
    public class Program
    {                
        public UnSatCores unSATCores;
        public Lemmas lemmas;
        public Random random;
        public Program(Random random)
        {
            this.random = random;
        }

        public UnSatCore CheckConflict(List<Z3ComponentSpecs> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<string> root, Grammar grammar)
        {
            var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SMTEncode(componentSpecs, context, programSpec, root, grammar);

            return SMTSolver.SMTSolve(context, satEncodedArtifactsAsSMTModel);
        }

        public Lemma AnalyzeConflict(UnSatCore unSATCore, List<Z3ComponentSpecs> z3ComponentsSpecs, Context context, TreeNode<string> root, Grammar grammar)
        {
            var lemma = new Lemma();
            foreach (var clause in unSATCore)
            {
                //var rule = programRoot.GetAtIndex(Int32.Parse(clause.index)).rule;
                var rule = grammar.DFS(root, (x) => x.index == Int32.Parse(clause.index)).rule;
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

        public TreeNode<string> BackTrack(UnSatCore unSATCore, Grammar grammar, TreeNode<string> currentNode, TreeNode<string> root)
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
                root = new TreeNode<string>();
                currentNode = root;
            }

            return root;
        }

        public void Synthesize(int demand)
        {
            var z3ComponentsSpecs = new List<Z3ComponentSpecs>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                var typeSpecs = TypeSpecBuilder.Build(Resources.path_typeSpec);
                var programSpec = ProgramSpecBuilder.Build(Resources.path_programSpec, context, typeSpecs);
                var grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(Resources.path_componentSpec, context, programSpec, grammar);

                var numberOfPrograms = 0;

                var root = new TreeNode<string>();
                lemmas = new Lemmas();
                var currentNode = root;
                while (true)
                {
                    currentNode = grammar.Decide(root, lemmas, context, grammar);
                    root.Visualize();
                    grammar.Propogate(root, lemmas, context, grammar);

                    var unSATCore = CheckConflict(z3ComponentsSpecs, context, programSpec, root, grammar);

                    if (unSATCore?.Count != 0)
                    {
                        var lemma = AnalyzeConflict(unSATCore, z3ComponentsSpecs, context, root, grammar);
                        lemmas.Add(lemma);

                        root = BackTrack(unSATCore, grammar, currentNode, root);
                    }

                    if (lemmas.IsUnSAT(context))
                        return;

                    if (root.IsConcrete)
                    {
                        Console.WriteLine("\nConcrete progam found:");
                        root.Visualize();
                        Console.WriteLine("#######################################");

                        if (lemmas.Count > 3)
                            ;

                        //var result = CreateRandomParamsAndExecuteProgram(root, new Random());
                        //ExecuteProgram(root, new object[] { new List<int> { 1, 34, 15, 6, 10 }, 2 });

                        root = new TreeNode<string>();
                        currentNode = root;
                        lemmas.Clear();
                        //unSATCores.Clear();
                        grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random);

                        if (numberOfPrograms + 1 == demand)
                            break;
                        else
                            numberOfPrograms++;
                    }
                }
            }
        }

        public void Synthesize_WhileTrue()
        {
            while (true)
            {
                Console.Write("Please specify the amount of concrete programs:");
                var numberOfPrograms = Convert.ToInt32(Console.ReadLine());
                Synthesize(numberOfPrograms);
            }
        }


        static void Main(string[] args)
        {
            var rand = new Random(5);
            var program = new Program(rand);
            program.Synthesize_WhileTrue();
            //BenchmarkFactory.CreateBenchmark(rand);

        }
    }

   
}