using CSharpTree;
using Microsoft.Z3;
using Synthesizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Synthesis
{
    public enum SymbolType
    {
        NonTerminal,
        Terminal
    }

    public class Grammar
    {
        public List<string> nonTerminals;
        public List<string> terminals;
        public List<Production> productions;
        public string startSymbol;
        public int maxArity;
        public List<Tuple<string, Parameter>> typeConstants;

        //private Random rand = new Random(1);
        private Random rand;


        public void addNonTerminalSymbol(string nonTerminalSymbol)
        {
            nonTerminals.Add(nonTerminalSymbol);
        }
        public void addTerminalSymbol(string terminalSymbol)
        {
            terminals.Add(terminalSymbol);
        }
        public Grammar()
        {
            nonTerminals = new List<string>();
            terminals = new List<string>();
            productions = new List<Production>();
        }
        public Grammar(string startSymbol, List<string> nonTerminals, List<string> terminals, List<Production> productions, int maxArity, List<Tuple<string, Parameter>> typeConstants, Random rand)
        {
            this.startSymbol = startSymbol;
            this.nonTerminals = nonTerminals;
            this.terminals = terminals;
            this.productions = productions;
            this.maxArity = maxArity;
            this.typeConstants = typeConstants;
            this.rand = rand;
        }
        public void addProduction(string lhs, List<string> rhs, int arity)
        {
            productions.Add(new Production(lhs, rhs, arity));
        }

        public void SetStartSymbol(string startSymbol)
        {
            this.startSymbol = startSymbol;
        }

        private bool ContainsNonTerminals(string program)
        {
            for (int i = 0; i < nonTerminals.Count; i++)
            {
                if (program.Contains(nonTerminals[i]))
                    return true;
            }
            return false;
        }

        public int calculateIndex2(int i, int k, int d)
        {
            return (int)(Math.Pow(k, d - 1) + i);
        }

        public void Propogate(TreeNode<string> root, Lemmas lemmas, Context context, Grammar grammar)
        {
            var satEncodedProgram = SATEncoder<string>.SATEncode(root, context);
            var lemmasIConjunction = lemmas.LemmasInConjunction(context);

            var rule1 = grammar.productions.First();

            var holes = root.Holes();

            foreach (var hole in holes)
            {
                var index = hole.Parent.Children.IndexOf(hole);
                var nonTerminalToExpand = hole.Parent.rule.rightHandSide[index + 1];
                var rules = grammar.productions.Where(x => x.leftHandSide == nonTerminalToExpand).ToList();

                var componentsFromRules_SATEncoded = rules.Select(x => context.MkBoolConst($"C_{hole.index}_{x.component}"));

                var or_componentsFromRules_SATEncoded = componentsFromRules_SATEncoded.Count() == 1 ? componentsFromRules_SATEncoded.First() : context.MkOr(componentsFromRules_SATEncoded);

                foreach (var rule in rules)
                {
                    var component_SATEncoded = context.MkBoolConst($"C_{hole.index}_{rule.component}");

                    var leftHandSide = context.MkAnd(new BoolExpr[] { satEncodedProgram, lemmasIConjunction, or_componentsFromRules_SATEncoded });
                    var check = context.MkNot(context.MkImplies(leftHandSide, component_SATEncoded));
                    if (SMTSolver.CheckIfUnSAT(context, check))
                    {
                        hole.FillHole(rule.component, rule, context, grammar);
                        Console.WriteLine("Propogate:");
                        //root.Visualize();
                        Propogate(root, lemmas, context, grammar);
                        return;
                    }

                }
            }
        }
        public TreeNode<string> Decide(TreeNode<string> currentNode, Lemmas lemmas, Context context, Grammar grammar, Params param)
        {
            currentNode = Decide_AST(currentNode, lemmas, context, grammar, param);
            return currentNode;
        }

        public Stack<TreeNode<string>> DFS(TreeNode<string> root, Func<TreeNode<string>, bool> predicate)
        {
            var stack = new Stack<TreeNode<string>>();
            var returnStack = new Stack<TreeNode<string>>();
            stack.Push(root);

            while (stack.Count() != 0)
            {
                var current = stack.Pop();
                returnStack.Push(current);

                if (predicate(current))
                {
                    return returnStack;
                }
                else
                {
                    for (var i = current.Children.Count() - 1; i >= 0; i--)
                        stack.Push(current.Children[i]);
                }
            }
            throw new ArgumentException("no node found for the given predicate");
        }

        public TreeNode<string> Decide_AST(TreeNode<string> root, Lemmas lemmas, Context context, Grammar grammar, Params param)
        {
            var hole = DFS(root, (x) => x.IsHole).Peek();

            var condition = root.holes == null;
            var currentLeftHandSide = condition ? grammar.startSymbol : hole.Parent.holes.Pop();

            if (!condition)
                hole.Parent.holesBackTrack.Push(currentLeftHandSide);

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();

            while (possibleProductionRules.Count > 0)
            {
                var index = rand.Next(0, (possibleProductionRules.Count()));
                //var index = 0;
                //var index = 1;
                var choosenProductionRule = possibleProductionRules.ElementAt(index);

                var terminal = choosenProductionRule.rightHandSide.First();

                var holeToFill = hole.IsHole ? hole : hole.Children.FirstOrDefault(x => x.IsHole);

                holeToFill.FillHole(terminal, choosenProductionRule, context, grammar);

                if (RuleResultsInLeaf(grammar, choosenProductionRule))
                {
                    var satEncodedProgram = SATEncoder<string>.SATEncode(root, context);
                    foreach (var lemma in lemmas)
                    {
                        //checking consistency with the knoweldge base
                        var lemmaAsExpersion = lemma.AsExpression(context);
                        var check = context.MkAnd(lemmaAsExpersion, satEncodedProgram);
                        var checkIfUnSAT = SMTSolver.CheckIfUnSAT(context, check);
                        if (checkIfUnSAT)
                        {
                            holeToFill.MakeHole();
                            possibleProductionRules.Remove(choosenProductionRule);
                            break;
                        }
                    }
                }
                if (!holeToFill.IsHole)
                {
                    if (!RuleResultsInLeaf(grammar, holeToFill.rule))
                    {
                        productions.Remove(holeToFill.rule);
                    }

                    return hole;
                }
            }
            return null;
        }


        public bool Check(TreeNode<string> root)
        {
            bool check = true;
            try
            {
                check = root.Data == "sum";
                var child = root.Children.First();
                check &= child.Data == "map";

                child = child.Children.First();
                check &= child.Data == "sort";

                child = child.Children.First();
                check &= child.Data == "scanL1";

                child = child.Children.First();
                check &= child.Data == "take";

                child = child.Children.First();
                check &= child.Data == "filter";

                child = child.Children.First();
                check &= child.Data == "x1";
            }
            catch (Exception ex)
            {
                check = false;
            }

            return check;
        }

        public bool Check1(TreeNode<string> root)
        {
            bool check = true;
            try
            {
                check = root.Data == "sum";
                var child = root.Children.First();
                check &= child.Data == "map";

                child = child.Children.First();
                check &= child.Data == "scanL1";

                child = child.Children.First();
                check &= child.Data == "filter";

                child = child.Children.First();
                check &= child.Data == "take";

                child = child.Children.First();
                check &= child.Data == "x1";

                //child = child.Children.First();
                //check &= child.Data == "[1'2'3]";
            }
            catch (Exception ex)
            {
                check = false;
            }

            return check;
        }

        public TreeNode<string> Decide_AST(TreeNode<string> root, ref List<TreeNode<string>> unSATCorePrograms,
            Context context, Grammar grammar, List<Z3ComponentSpecs> z3ComponentsSpecs, ProgramSpec programSpec,
            ref Lemmas lemmas, ref int lemmaCounter, ref int extensionCounter, ref List<long> pruningTimes, Params param)
        {
            var searchStack = DFS(root, (x) => x.IsHole);
            var hole = searchStack.Pop();

            string currentLeftHandSide;

            var condition = (hole.holes == null || hole.holes.Count == 0) && hole.IsRoot;
            if (condition)
            {
                currentLeftHandSide = grammar.startSymbol;
            }
            else
            {
                currentLeftHandSide = hole.Parent.holes.Pop();
                hole.Parent.holesBackTrack.Push(currentLeftHandSide);
            }

            //var possibleProductionRules1 = productions.Where(x => x.leftHandSide == currentLeftHandSide &&
            //    !hole.deadends.Any(y => y == x.rightHandSide.First())).ToList();
            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();

            var holeToFill = new TreeNode<string>();

            holeToFill = hole.IsHole ? hole : hole.Children.FirstOrDefault(x => x.IsHole);
            while (possibleProductionRules.Count > 0)
            {
                int index;
                if (param.random)
                    index = rand.Next(0, (possibleProductionRules.Count()));
                else
                    index = 0;

                var choosenProductionRule = possibleProductionRules.ElementAt(index);

                var terminal = choosenProductionRule.rightHandSide.First();

                holeToFill.FillHole(terminal, choosenProductionRule, context, grammar);

                if (Check(root))
                    ;


                //if (RuleResultsInLeaf(grammar, choosenProductionRule))
                //{
                var stopWatch = new Stopwatch();
                var elapsedTime_Base = default(long);
                var elapsedTime_Extension = default(long);

                #region reject with base-lemmas
                if (param.use_base_lemmas || (param.use_extended_lemmas && !param.use_base_lemmas))
                {
                    stopWatch.Start();
                    //Reject current partial program using Lemmas
                    var satEncodedProgram = SATEncoder<string>.SATEncode(root, context);

                    var lemmasAsExp = lemmas.Select(x => x.AsExpression(context)).ToList();
                    var lemmasAsConj = context.MkAnd(lemmasAsExp);

                    //foreach (var lemma in lemmas)
                    //{
                    //checking consistency with the knoweldge base (Lemmas)
                    //var lemmaAsExpersion = lemma.AsExpression(context);

                    var check = context.MkAnd(lemmasAsConj, satEncodedProgram);
                    var checkIfUnSAT = SMTSolver.CheckIfUnSAT(context, check);

                    if (checkIfUnSAT)
                    {
                        holeToFill.MakeHole();
                        possibleProductionRules.Remove(choosenProductionRule);
                        lemmaCounter++;
                        extensionCounter++;
                        //break;
                    }
                    //}

                    stopWatch.Stop();
                    elapsedTime_Base = stopWatch.ElapsedMilliseconds;
                    stopWatch.Reset();
                }
                #endregion

                #region reject with extended-lemmas
                if (param.use_extended_lemmas)
                {
                    stopWatch.Start();
                    //Reject current partial program using unSATPrograms
                    foreach (var unSATCoreProgram in unSATCorePrograms)
                    {
                        //checking consistency with the knoweldge base (UnSAT Programs)
                        var program = new Program(rand);

                        //var unSATCores = program.CheckConflict(z3ComponentsSpecs, context, programSpec, root, grammar);
                        //var unSATCore = program.CheckConflict(z3ComponentsSpecs, context, programSpec, unSATCoreProgram, grammar);

                        var unSATPorgram = test(unSATCoreProgram, grammar, z3ComponentsSpecs)
                            .SplitBy(LogicalOperators.operators[ELogicalOperators.AND])
                            .Select(x => ComponentSpecsBuilder.GetComponentSpec(new Z3ComponentSpecs()
                            {
                                key = x,
                                value = x
                            }))
                            .SelectMany(x => x).ToList();
                        var candidateProgram = test(root, grammar, z3ComponentsSpecs)
                            .SplitBy(LogicalOperators.operators[ELogicalOperators.AND])
                            .Select(x => ComponentSpecsBuilder.GetComponentSpec(new Z3ComponentSpecs()
                            {
                                key = x,
                                value = x
                            }))
                            .SelectMany(x => x).ToList();

                        var satEncodedArtifactsAsSMTModel_1 = SATEncoder<string>.SMTEncode(z3ComponentsSpecs, context, programSpec, root, grammar, Symbols.ivs);
                        var satEncodedArtifactsAsSMTModel_2 = SATEncoder<string>.SMTEncode(z3ComponentsSpecs, context, programSpec, unSATCoreProgram, grammar, "r");

                        //var candidateProgram = satEncodedArtifactsAsSMTModel_1.satEncodedProgram.SelectMany(x => x.clauses.First).ToArray();
                        //var unSATPorgram = satEncodedArtifactsAsSMTModel_2.satEncodedProgram.SelectMany(x => x.clauses.First).ToArray();



                        var check = context.MkNot(context.MkImplies(context.MkAnd(candidateProgram), context.MkAnd(unSATPorgram)));
                        var checkIfUnSAT = SMTSolver.CheckIfUnSAT(context, check);

                        if (checkIfUnSAT)
                        {
                            holeToFill.MakeHole();
                            possibleProductionRules.Remove(choosenProductionRule);
                            extensionCounter++;
                            break;
                        }
                    }
                    stopWatch.Stop();
                    elapsedTime_Extension = stopWatch.ElapsedMilliseconds;
                    stopWatch.Reset();
                }
                #endregion

                var ratio = (extensionCounter == 0 || lemmaCounter == 0) ? 1 : extensionCounter / lemmaCounter;
                //Console.WriteLine($"Extension/Lemma ratio:{ratio}");

                pruningTimes.Add(elapsedTime_Base - elapsedTime_Extension);
                //Console.WriteLine($"{lemmas.Count == 0} {unSATCorePrograms.Count == 0} Elapsed time base - extension: {elapsedTime_Base - elapsedTime_Extension}");
                //}
                if (!holeToFill.IsHole)
                {
                    if (!RuleResultsInLeaf(grammar, holeToFill.rule))
                    {
                        productions.Remove(holeToFill.rule);
                    }
                    return holeToFill;
                }
            }

            if(param.printConsole)
                root.Visualize();
            //File.AppendAllText(Resources.path_results, root.ToString());


            //holeToFill.deadends.Clear();
            holeToFill.Parent.holes.Push(holeToFill.Parent.holesBackTrack.Pop());

            holeToFill = searchStack.Pop();

            //holeToFill.deadends.Add(holeToFill.Data);

            if (param.use_base_lemmas)
            {
                var lemma = Lemma.NewLemma(root, context);

                var lemmasAsExpression = lemma.AsExpression(context);


                var lemmaAsString = CheckLemma_ByString(lemma);
                lemmas.RemoveAll(x => CheckLemma_ByString(x).Contains(lemmaAsString));

                //lemmas.RemoveAll(x => CheckLemma(lemma, x, context));

                var count = lemmas.Where(x => x.AsExpression(context) == lemma.AsExpression(context)).Count();
                if (count == 0)
                    lemmas.Add(lemma);

            }

            if (!RuleResultsInLeaf(grammar, holeToFill.rule))
            {
                grammar.productions.Add(holeToFill.rule);
            }
            holeToFill.MakeHole();

            //currentLeftHandSide = holeToFill.Parent.holesBackTrack.Peek();

            holeToFill.Parent.holes.Push(holeToFill.Parent.holesBackTrack.Pop());
            return Decide_AST(root, ref unSATCorePrograms, context, grammar, z3ComponentsSpecs,
                programSpec, ref lemmas, ref lemmaCounter, ref extensionCounter, ref pruningTimes, param);

        }

        public bool CheckLemma_ByImplication(Lemma newLemma, Lemma oldLemma, Context context)
        {
            return SMTSolver.CheckIfUnSAT(context, context.MkNot(context.MkImplies(newLemma.AsExpression(context), oldLemma.AsExpression(context))));
        }

        public string CheckLemma_ByString(Lemma lemma)
        {
            var t2 = lemma.Select(x => x.Select(y => y.Args.First().ToString()).First()).ToList();
            var temp_1 = String.Join(" ", t2);

            return temp_1;
        }
        public string test(TreeNode<string> root, Grammar grammar, List<Z3ComponentSpecs> z3ComponentSpecs)
        {
            var spec = root.getSpecAsString(grammar, z3ComponentSpecs);
            var root_constraints = spec.SplitBy(LogicalOperators.operators[ELogicalOperators.AND]).Where(x => x.Contains("y")).ToList();

            var newSpecList = new List<string>();

            foreach (var constraint in root_constraints)
            {
                var newSpec = recurApply(constraint, spec);
                if (newSpec != null)
                    newSpecList.Add(newSpec);
            }
            return String.Join($" {LogicalOperators.operators[ELogicalOperators.AND]} ", newSpecList);
        }


        public string recurApply(string constraint, string spec)
        {
            var transitive_reductions = new List<string>();
            var newSpec = constraint;
            var s = spec.SplitBy(LogicalOperators.operators[ELogicalOperators.AND]).Select(x => x.SplitBy(" ").First()).ToList();
            if (s.Contains(newSpec.SplitBy(" ").Last()))
            {
                var root_splits = newSpec.SplitBy(" ").ToList();
                var child_constraints = spec.SplitBy(LogicalOperators.operators[ELogicalOperators.AND]).
                    Where(x => x.SplitBy(" ").First().Contains(root_splits.Single(y => y.Contains("v")))).ToList();

                foreach (var child_constraint in child_constraints)
                {
                    var newSpec1 = test1(newSpec, spec, child_constraint);
                    if (newSpec1 == null)
                        continue;
                    newSpec1 = recurApply(newSpec1, spec);
                    if (newSpec1 != null)
                        transitive_reductions.Add(newSpec1);
                }

            }
            else
                transitive_reductions.Add(newSpec);

            if (newSpec == null)
                return null;

            if (transitive_reductions.Count(x => x == null) != 0)
                ;
            transitive_reductions.RemoveAll(x => x.SplitBy(" ").Last().Contains("v"));
            if (transitive_reductions.Count == 0)
                return null;

            //return newSpec;
            return String.Join($" {LogicalOperators.operators[ELogicalOperators.AND]} ", transitive_reductions);
        }

        public string test1(string root_constraint, string spec, string child_constraint)
        {

            if (!root_constraint.Contains("v"))
                return null;
            var root_splits = root_constraint.SplitBy(" ").ToList();
            var first_operator = RelationalOperators.operators.FirstOrDefault(x => x.Value == root_splits[1]).Key;
            root_splits.Remove(root_splits[1]);
            var arg_1 = root_splits.Single(x => !x.Contains("v"));

            var child_splits = child_constraint.SplitBy(" ").ToList();

            var second_operator = RelationalOperators.operators.FirstOrDefault(x => x.Value == child_splits[1]).Key;
            child_splits.Remove(child_splits[1]);
            child_splits.Remove(root_splits.Single(y => y.Contains("v")));
            var transitive_rediction = s(root_splits.First(), child_splits.First(), new List<ERelationalOperators>() { first_operator, second_operator });

            if (transitive_rediction == "true")
                return null;

            return transitive_rediction;

            //return String.Join($" {LogicalOperators.operators[ELogicalOperators.AND]} ", transitive_reductions);

        }

        public string s(string s1, string s2, List<ERelationalOperators> operators)
        {

            if (operators.First() == operators.Last())
                return $"{s1} {RelationalOperators.operators[operators.First()]} {s2}";
            else if (operators.Any(x => x == ERelationalOperators.Eq))
            {
                var opr = operators.Where(x => x == ERelationalOperators.Eq).First();
                operators.Remove(opr);

                return $"{s1} {RelationalOperators.operators[operators.First()]} {s2}";
            }
            else if (operators.Any(x => x == ERelationalOperators.Gt) && operators.Any(x => x == ERelationalOperators.GtEq))
            {
                return $"{s1} {RelationalOperators.operators[ERelationalOperators.Gt]} {s2}";
            }
            else if (operators.Any(x => x == ERelationalOperators.LEq) && operators.Any(x => x == ERelationalOperators.L))
            {
                return $"{s1} {RelationalOperators.operators[ERelationalOperators.L]} {s2}";
            }
            else
                return "true";
        }


        public bool RuleResultsInLeaf(Grammar grammar, Production rule)
        {
            var check = (grammar.nonTerminals.Where(x => rule.rightHandSide.Contains(x))?.Count() == 0);
            return check;
        }

        private bool IsTerminal(string rhs)
        {
            return terminals.Contains(rhs);
        }
        private bool IsNonTerminal(string rhs)
        {
            return nonTerminals.Contains(rhs);
        }

        private void generateRandomAssignments_ParseTree(TreeNode<string> currentNode)
        {
            var currentLeftHandSide = currentNode.Data;

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();
            var index = rand.Next(0, (possibleProductionRules.Count()));
            var choosenProductionRule = possibleProductionRules.ElementAt(index);

            foreach (var rhs in choosenProductionRule.rightHandSide)
            {
                var newChildNode = currentNode.AddChild(rhs, choosenProductionRule.arity);
                generateRandomAssignments_ParseTree(newChildNode);
            }
        }
    }
}