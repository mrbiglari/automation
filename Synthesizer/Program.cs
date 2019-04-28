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
        public const string specsFolderPath = "Specs/";
        public const string path_grammarSpec = specsFolderPath + "GrammarSpec.xml";
        public const string path_componentSpec = specsFolderPath + "ComponentSpecs.xml";
        public const string path_programSpec = specsFolderPath + "ProgramSpec.xml";
        public const string path_typeSpec = specsFolderPath + "TypeSpec.xml";
        public Random rand = new Random(5);
        public UnSatCores unSATCores;
        public Lemmas lemmas;


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
                var typeSpecs = TypeSpecBuilder.Build(path_typeSpec);
                var programSpec = ProgramSpecBuilder.Build(path_programSpec, context, typeSpecs);
                var grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs, rand);
                z3ComponentsSpecs = ComponentSpecsBuilder.Build(path_componentSpec, context, programSpec, grammar);

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

                        ExecuteProgram(root, new object[] { new List<int> { 1, 34, 15, 6, 10 }, 2 });

                        root = new TreeNode<string>();
                        currentNode = root;
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

        public void Synthesize_WhileTrue()
        {
            while (true)
            {
                Console.Write("Please specify the amount of concrete programs:");
                var numberOfPrograms = Convert.ToInt32(Console.ReadLine());
                Synthesize(numberOfPrograms);
            }
        }

        public void TEMP()
        {
            using (Context context = new Context(new Dictionary<string, string>() { { "proof", "true" } }))
            {
                var typeSpecs = TypeSpecBuilder.Build(path_typeSpec);
                var grammar = GrammarBuilder.Build(path_grammarSpec, typeSpecs, rand);
                var root = new TreeNode<string>();
                while (true)
                {
                    grammar.Decide(root, new Lemmas(), context, grammar);
                    if (root.IsConcrete)
                    {
                        root.Visualize();
                        break;
                    }
                    //var type = Type.GetType("ComponentConcreteSpecs");
                    //var type = Type.GetType(typeof(ComponentConcreteSpecs).Name);                    
                }
            }
        }

        public void ImplementProgram()
        {
            var methodName = "last";
            var type = typeof(ComponentConcreteSpecs);
            //var method = type.GetMethod(methodName, BindingFlags.Public);
            var method = type.GetMethod(methodName);
            var args = method.GetGenericArguments();
            var result = method.Invoke(null, new object[] { new List<int> { 1, 2, 3 } });
        }
        

        public object ExecuteProgram(TreeNode<string> node, object[] programArguments)
        {
            var methodName = node.Data;
            var type = typeof(ComponentConcreteSpecs);
            var method = type.GetMethod(methodName);
            var args = new List<object>();
            //var ss = method.ReturnType;
            //var sss = method.ReflectedType;
            //var s1 = Delegate.CreateDelegate(typeof(ComponentConcreteSpecs), method);
            //var s1 = Delegate.CreateDelegate(Type.GetType(method), method);
            //var a1= s1.DynamicInvoke(new List<int>() { 1,2,3});

            //var sss = method.CreateDelegate(this);

            //var a1 = sss.DynamicInvoke(new List<int>() { 1, 2, 3 });

            foreach (var child in node.Children)
            {
                args.Add(ExecuteProgram(child, programArguments));
            }

            if(node.Children.Count == 0)
            {
                if (node.Data.Contains("x"))
                {
                    return programArguments[Int32.Parse(node.Data.SplitBy("x").Last()) - 1];
                }
                else if (method != null)
                    return method.CreateDelegate(this);
                else
                    return Int32.Parse(node.Data);
            }
            else           
                return method.Invoke(null, args.ToArray());


        }

        static void Main(string[] args)
        {
            var program = new Program();
            program.Synthesize_WhileTrue();
            //program.ImplementProgram();
            //program.TEMP();

        }
    }

    public static class DelegateExtension
    {
        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }
    }
}