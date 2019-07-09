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
                var temp = grammarGround.DFS(rootOfUnSATCoreProgram, x => x.index == node.index.ToInt()).Pop();
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
                var rule = grammar.DFS(root, (x) => x.index == Int32.Parse(clause.index)).Pop().rule;
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


        public void SynthesisReset(ref TreeNode<string> root, ref Lemmas lemmas, ref List<TreeNode<string>> unSATCorePrograms)
        {
            root = new TreeNode<string>();
            lemmas = new Lemmas();
            unSATCorePrograms = new List<TreeNode<string>>();
        }


        public TreeNode<string> Synthesize(int demand, Params param, Context context, SynthesisParams synthesisParams)
        {

            lemmaCreationTimes = new List<long>();
            pruningTimes = new List<long>();
            var root = new TreeNode<string>();
            lemmas = new Lemmas();
            unSATCorePrograms = new List<TreeNode<string>>();
            var currentNode = root;
            while (true)
            {
                //currentNode = grammar.Decide(root, lemmas, context, grammar);
                 currentNode = synthesisParams.grammar.Decide_AST(root, ref unSATCorePrograms, context, synthesisParams.grammar,
                    synthesisParams.z3ComponentSpecs, synthesisParams.programSpec, ref lemmas, ref lemmaCounter, ref extensionCounter, 
                    ref pruningTimes, param);
                root.Visualize();
                //synthesisParams.grammar.Propogate(root, lemmas, context, synthesisParams.grammar);

                var unSATCore = CheckConflict(synthesisParams.z3ComponentSpecs, context, synthesisParams.programSpec, root, synthesisParams.grammar);

                if (unSATCore?.Count == 1)
                {
                    ;
                }
                    if (unSATCore?.Count != 0)
                {
                    var stopWatch = new Stopwatch();
                    var elapsedTime_Base = default(long);
                    var elapsedTime_Extension = default(long);

                    if (param.use_base_lemmas)
                    {
                        stopWatch.Start();

                        //creating lemma from UnSATCore
                        var lemma = AnalyzeConflict(unSATCore, synthesisParams.z3ComponentSpecs, context, root, synthesisParams.grammar);
                        lemmas.Add(lemma);

                        stopWatch.Stop();
                        elapsedTime_Base = stopWatch.ElapsedMilliseconds;
                        stopWatch.Reset();
                    }

                    if (param.use_extended_lemmas)
                    {
                        stopWatch.Start();

                        //creating unSAT Programs from UnSATCore
                        var rootOfUnSATCoreProgram = ExtractUnSATProgram(unSATCore, synthesisParams.grammarGround, context);
                        unSATCorePrograms.Add(rootOfUnSATCoreProgram);

                        stopWatch.Stop();
                        elapsedTime_Extension = stopWatch.ElapsedMilliseconds;
                    }

                    if (elapsedTime_Base != 0 && elapsedTime_Extension != 0)
                        lemmaCreationTimes.Add(elapsedTime_Base - elapsedTime_Extension);

                    //Console.WriteLine($"{lemmas.Count == 0} {unSATCorePrograms.Count == 0} Elapsed time base - extension: {elapsedTime_Base - elapsedTime_Extension}");

                    root = BackTrack(unSATCore, synthesisParams.grammar, currentNode, root);
                }

                if (lemmas.IsUnSAT(context))
                    return null;

                else if (root.IsConcrete)
                {
                    if (param.find_groundTruth)
                    {
                        var program_as_string = SAT_Encode(root, context);
                        if (!program_as_string.Equals(synthesisParams.programSpec.program))
                        {

                            if(param.use_base_lemmas)
                            {
                                var lemma = Lemma.NewLemma(root, context);

                                lemmas.Add(lemma);
                            }

                            root = new TreeNode<string>();
                            synthesisParams.grammar = GrammarBuilder.Build(Resources.path_grammarSpec, synthesisParams.typeSpecs, random, synthesisParams.programSpec.parameters);
                            continue;
                        }
                    }

                    var benchmark_Id = Resources.path_programSpec.Replace(".xml", $"{synthesisParams.benchmarkId}.xml");
                    Console.WriteLine($"\nConcrete progam found for benchmark {benchmark_Id}:");
                    root.Visualize();                    
                    Console.WriteLine($"####################################### ");                      

                    return root;
                }
            }

        }


        public void Synthesize_WhileTrue(Params param)
        {
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {                
                var benchmark_count = Directory.GetFiles(Resources.path_programSpec_base).Length;

                for (int benchmark_id = 1; benchmark_id <= benchmark_count; benchmark_id++)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    var numberOfPrograms = 1;

                    var typeSpecs = TypeSpecBuilder.Build(Resources.path_typeSpec);
                    var programSpec = ProgramSpecBuilder.Build(Resources.path_programSpec_x.Replace(".xml", $"{benchmark_id}.xml"), context, typeSpecs);
                    var grammar = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);
                    var grammarGround = GrammarBuilder.Build(Resources.path_grammarSpec, typeSpecs, random, programSpec.parameters);
                    var z3ComponentsSpecs = ComponentSpecsBuilder.Build(Resources.path_componentSpec, context, programSpec, grammar);

                    var synthesisParams = new SynthesisParams()
                    {
                        typeSpecs = typeSpecs,
                        programSpec = programSpec,
                        grammar = grammar,
                        grammarGround = grammarGround,
                        z3ComponentSpecs = z3ComponentsSpecs,
                        benchmarkId = benchmark_id
                    };

                    var roots = new List<TreeNode<string>>();
                    for (int i = 0; i < numberOfPrograms; i++)
                    {
                        var root = Synthesize(numberOfPrograms, param, context, synthesisParams);
                        roots.Add(root);
                    }

                    stopWatch.Stop();
                    Console.WriteLine($"Time Elapsed: {(double)stopWatch.Elapsed.TotalSeconds}");

                    
                    if (numberOfPrograms == 1)
                    {
                        var root = roots.First();
                        string createText = $"{stopWatch.Elapsed.TotalSeconds.ToString()} {benchmark_id} {lemmas.Count} {unSATCorePrograms.Count} {root.Size} {SAT_Encode(root, context)}\n";
                        if (benchmark_id == 1)
                        {
                            File.WriteAllText(Resources.path_results, String.Empty);
                            File.AppendAllText(Resources.path_results, "{stopWatch.Elapsed.TotalSeconds.ToString()} {benchmark_id} {lemmas.Count} {unSATCorePrograms.Count} {root.Size} {SAT_Encode(root, context)}\n");
                        }
                        File.AppendAllText(Resources.path_results, createText);
                    }

                    if (param.debug)
                    {
                        Console.WriteLine($"Press Enter to continue");
                        Console.ReadLine();
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            var param = new Params() { use_base_lemmas = true, find_groundTruth = true };
            
            var rand = new Random(2);
            var program = new Program(rand);

            Console.WriteLine("Enter options below:");
            Console.WriteLine("1- Syntheisze from benchmarks");
            Console.WriteLine("2- Generate benchmarks");

            var option = Console.ReadLine();

            if(option == "1")
                program.Synthesize_WhileTrue(param);
            else if (option == "1d")
                {
                    param.debug = true;
                    program.Synthesize_WhileTrue(param);
                }
                    
            else if (option == "2")
                BenchmarkFactory.CreateBenchmark(rand);

        }
    }

    public class Params
    {
        public bool use_extended_lemmas = false;
        public bool use_base_lemmas = false;
        public bool find_groundTruth = false;
        public bool debug = false;
    }

    public class SynthesisParams
    {
        public List<TypeSpec> typeSpecs;
        public ProgramSpec programSpec;

        public Grammar grammar;
        public Grammar grammarGround;
        public List<Z3ComponentSpecs> z3ComponentSpecs;
        public int benchmarkId;
    }

}