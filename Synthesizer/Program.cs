using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public List<TreeNode<string>> unSATCorePrograms;
        public Lemmas lemmas;
        public Random random;
        public int lemmaCounter;
        public int extensionCounter;
        public List<long> pruningTimes;
        public List<long> lemmaCreationTimes;
        public Program(Random random)
        {
            this.random = random;
        }


        public TreeNode<string> ExtractUnSATProgram(UnSatCore unSATCore, Grammar grammarGround, Context context)
        {
            var minIndex = unSATCore.Min(x => x.index.ToInt());
            var rootCores = unSATCore.Where(x => x.index.ToInt() == minIndex).ToList();

            var result = unSATCore.GroupBy(x => x.index).Select(grp => grp.First()).OrderBy(x => x.index.ToInt()).ToList();

            var rule = grammarGround.productions.Where(x => x.component == rootCores.First().name).First();

            var rootOfUnSATCoreProgram = new TreeNode<string>();
            rootOfUnSATCoreProgram.FillHole(rule.component, rule, context, grammarGround, minIndex);

            foreach (var node in result.Skip(1))
            {
                rule = grammarGround.productions.Where(x => x.component == node.name).First();
                var temp = grammarGround.DFS(rootOfUnSATCoreProgram, x => x.index == node.index.ToInt());
                temp.FillHole(rule.component, rule, context, grammarGround);
            }

            return rootOfUnSATCoreProgram;
        }

        public UnSatCore CheckConflict(List<Z3ComponentSpecs> componentSpecs, Context context, ProgramSpec programSpec, TreeNode<string> root, Grammar grammar)
        {
            var satEncodedArtifactsAsSMTModel = SATEncoder<string>.SMTEncode(componentSpecs, context, programSpec, root, grammar, Symbols.ivs);

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

        public static string SAT_Encode(TreeNode<string> root, Context context)
        {
            var sat_encoded_program = SATEncoder<string>.SATEncode(root, context);            
            var sat_encoded_pogram_as_string = sat_encoded_program.Args.ToList().Select(x => x.ToString()).OrderBy(x => Int32.Parse(x.SplitBy("_").ElementAt(1))).ToList();
            return String.Join(" ", sat_encoded_pogram_as_string);
        }

        public void Synthesize(int demand)
        {
            var z3ComponentsSpecs = new List<Z3ComponentSpecs>();
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                for (int i = 1; i <= 100; i++)
                {
                    var stopWatch1 = new Stopwatch();
                    stopWatch1.Start();

                    var typeSpecs = TypeSpecBuilder.Build(Resources.path_typeSpec);
                    var programSpec = ProgramSpecBuilder.Build(Resources.path_programSpec.Replace(".xml", $"{i}.xml"), context, typeSpecs);
                    var grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);
                    var grammarGround = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);
                    z3ComponentsSpecs = ComponentSpecsBuilder.Build(Resources.path_componentSpec, context, programSpec, grammar);

                    var numberOfPrograms = 0;
                    lemmaCreationTimes = new List<long>();
                    pruningTimes = new List<long>();
                    var root = new TreeNode<string>();
                    lemmas = new Lemmas();
                    unSATCorePrograms = new List<TreeNode<string>>();
                    var currentNode = root;
                    while (true)
                    {
                        //currentNode = grammar.Decide(root, lemmas, context, grammar);
                        currentNode = grammar.Decide_AST(root, unSATCorePrograms, context, grammar, z3ComponentsSpecs, programSpec, lemmas, ref lemmaCounter, ref extensionCounter, ref pruningTimes);
                        root.Visualize();
                        grammar.Propogate(root, lemmas, context, grammar);

                        var unSATCore = CheckConflict(z3ComponentsSpecs, context, programSpec, root, grammar);

                        if (unSATCore?.Count != 0)
                        {
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();

                            //creating lemma from UnSATCore
                            //var lemma = AnalyzeConflict(unSATCore, z3ComponentsSpecs, context, root, grammar);
                            //lemmas.Add(lemma);

                            var elapsedTime_Base = stopWatch.ElapsedMilliseconds;
                            stopWatch.Reset();
                            stopWatch.Start();

                            //creating unSAT Programs from UnSATCore
                            var rootOfUnSATCoreProgram = ExtractUnSATProgram(unSATCore, grammarGround, context);
                            unSATCorePrograms.Add(rootOfUnSATCoreProgram);

                            var elapsedTime_Extension = stopWatch.ElapsedMilliseconds;
                            lemmaCreationTimes.Add(elapsedTime_Base - elapsedTime_Extension);
                            //Console.WriteLine($"{lemmas.Count == 0} {unSATCorePrograms.Count == 0} Elapsed time base - extension: {elapsedTime_Base - elapsedTime_Extension}");

                            root = BackTrack(unSATCore, grammar, currentNode, root);
                        }

                        if (lemmas.IsUnSAT(context))
                            return;

                        if (root.IsConcrete)
                        {
                            var program_as_string = SAT_Encode(root, context);

                            //if (!program_as_string.Equals(programSpec.program))
                            //{
                            //    root = new TreeNode<string>();
                            //    grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);
                            //    continue;
                            //}
                                
                            Console.WriteLine("\nConcrete progam found:");
                            //root.Visualize();
                            var benchmark_Id = Resources.path_programSpec.Replace(".xml", $"{i}.xml");
                            Console.WriteLine($"####################################### {benchmark_Id}");

                            //var ratio = (extensionCounter == 0 || lemmaCounter == 0) ? 0 : extensionCounter / lemmaCounter;
                            //var lemmaCreationAvg = (lemmaCreationTimes.Count() != 0) ? lemmaCreationTimes.Average() : 0;
                            //var pruningTimesAvg = (pruningTimes.Count() != 0) ? pruningTimes.Average() : 0;
                            //string createText = $"{lemmas.Count() + extensionCounter} {unSATCorePrograms.Count()} {ratio} {lemmaCreationAvg} {pruningTimesAvg}" + Environment.NewLine;
                            stopWatch1.Stop();
                            
                            string createText = $"{stopWatch1.Elapsed.TotalSeconds.ToString()} {i} {lemmas.Count} {root.Size} {program_as_string}\n";
                            File.AppendAllText("C:\\NewFolder\\results.txt", createText);
                            lemmaCounter = 0;
                            extensionCounter = 0;
                            lemmaCreationTimes.Clear();
                            pruningTimes.Clear();

                            if (lemmas.Count > 3)
                                ;

                            //var result = CreateRandomParamsAndExecuteProgram(root, new Random());
                            //ExecuteProgram(root, new object[] { new List<int> { 1, 34, 15, 6, 10 }, 2 });

                            root = new TreeNode<string>();
                            currentNode = root;
                            lemmas.Clear();
                            unSATCorePrograms.Clear();
                            //unSATCores.Clear();
                            grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);

                            if (numberOfPrograms + 1 == demand)
                                break;
                            else
                                numberOfPrograms++;
                        }
                    }
                }
            }
        }

        public void Synthesize_WhileTrue()
        {
            //while (true)
            //{
                //Console.Write("Please specify the amount of concrete programs:");
                //var numberOfPrograms = Convert.ToInt32(Console.ReadLine());
                var numberOfPrograms = 1;
                Synthesize(numberOfPrograms);
                Console.WriteLine($"Time Elapsed: {stopwatch.Elapsed.Seconds}");
            //}
        }

        public Stopwatch stopwatch;
        static void Main(string[] args)
        {
            
            //var rand = new Random(6);
            var rand = new Random(2);
            var program = new Program(rand);
            program.stopwatch = new Stopwatch();
            program.stopwatch.Start();
            program.Synthesize_WhileTrue();
            //BenchmarkFactory.CreateBenchmark(rand);

        }
    }

   
}