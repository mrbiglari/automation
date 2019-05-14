using CSharpTree;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                        root.Visualize();
                        Propogate(root, lemmas, context, grammar);
                        return;
                    }

                }
            }
        }
        public TreeNode<string> Decide(TreeNode<string> currentNode, Lemmas lemmas, Context context, Grammar grammar)
        {
            currentNode = Decide_AST(currentNode, lemmas, context, grammar);
            return currentNode;
        }

        public TreeNode<string> DFS(TreeNode<string> root, Func<TreeNode<string>, bool> predicate)
        {
            var stack = new Stack<TreeNode<string>>();
            stack.Push(root);

            while (stack.Count() != 0)
            {
                var current = stack.Pop();
                if (predicate(current))
                {
                    return current;
                }
                else
                {
                    for (var i = current.Children.Count() - 1; i >= 0; i--)
                        stack.Push(current.Children[i]);
                }
            }
            throw new ArgumentException("no holes found");
        }

        public TreeNode<string> Decide_AST(TreeNode<string> root, Lemmas lemmas, Context context, Grammar grammar)
        {
            var hole = DFS(root, (x) => x.IsHole);

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

        public TreeNode<string> Decide_AST(TreeNode<string> root, List<TreeNode<string>> unSATCorePrograms, Context context, Grammar grammar, List<Z3ComponentSpecs> z3ComponentsSpecs, ProgramSpec programSpec, Lemmas lemmas, ref int lemmaCounter, ref int extensionCounter, ref List<long> pruningTimes)
        {
            var hole = DFS(root, (x) => x.IsHole);

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
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    //Exclude using Lemmas
                    var satEncodedProgram = SATEncoder<string>.SATEncode(root, context);
                    foreach (var lemma in lemmas)
                    {
                        //checking consistency with the knoweldge base (Lemmas)
                        var lemmaAsExpersion = lemma.AsExpression(context);
                        var check = context.MkAnd(lemmaAsExpersion, satEncodedProgram);
                        var checkIfUnSAT = SMTSolver.CheckIfUnSAT(context, check);
                        if (checkIfUnSAT)
                        {
                            holeToFill.MakeHole();
                            possibleProductionRules.Remove(choosenProductionRule);
                            lemmaCounter++;
                            extensionCounter++;
                            break;
                        }
                    }
                    var elapsedTime_Base = stopWatch.ElapsedMilliseconds;
                    stopWatch.Reset();
                    stopWatch.Start();
                    //Exclude using unSATPrograms
                    foreach (var unSATCoreProgram in unSATCorePrograms)
                    {
                        //checking consistency with the knoweldge base (UnSAT Programs)
                        var satEncodedArtifactsAsSMTModel_1 = SATEncoder<string>.SMTEncode(z3ComponentsSpecs, context, programSpec, root, grammar, Symbols.ivs);
                        var satEncodedArtifactsAsSMTModel_2 = SATEncoder<string>.SMTEncode(z3ComponentsSpecs, context, programSpec, unSATCoreProgram, grammar, "r");

                        var candidateProgram = satEncodedArtifactsAsSMTModel_1.satEncodedProgram.SelectMany(x => x.clauses.First).ToArray();
                        var unSATPorgram = satEncodedArtifactsAsSMTModel_2.satEncodedProgram.SelectMany(x => x.clauses.First).ToArray();

                        var check = context.MkNot(context.MkImplies(context.MkAnd(candidateProgram.ToList().ToArray()), context.MkAnd(unSATPorgram)));
                        var checkIfUnSAT = SMTSolver.CheckIfUnSAT(context, check);
                        if (checkIfUnSAT)
                        {
                            holeToFill.MakeHole();
                            possibleProductionRules.Remove(choosenProductionRule);
                            extensionCounter++;
                            break;
                        }
                    }
                    var ratio = (extensionCounter == 0 || lemmaCounter == 0) ? 1 : extensionCounter / lemmaCounter;                    
                    Console.WriteLine($"Extension/Lemma ratio:{ratio}");
                    var elapsedTime_Extension = stopWatch.ElapsedMilliseconds;
                    pruningTimes.Add(elapsedTime_Base - elapsedTime_Extension);
                    //Console.WriteLine($"{lemmas.Count == 0} {unSATCorePrograms.Count == 0} Elapsed time base - extension: {elapsedTime_Base - elapsedTime_Extension}");
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