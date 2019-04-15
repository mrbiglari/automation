using CSharpTree;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
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
            for(int i = 0; i < nonTerminals.Count; i++)
            {
                if (program.Contains(nonTerminals[i]))
                    return true;
            }
            return false;
        }

        public int calculateIndex(TreeNode<string> node)
        {
            int index = 0;
            if(node.Parent != null)
            {
                var parentPositionInRow = ((node.Parent.index) * maxArity);
                var currentNodePositionInParentsChildrenList = (node.Parent.Children.ToList().IndexOf(node));
                index = currentNodePositionInParentsChildrenList + parentPositionInRow + 1;
            }
            return index;
        }

        public int calculateIndex2(int i, int k, int d)
        {
            return (int)(Math.Pow(k, d - 1) + i);
        }

        public void generateIndexes(TreeNode<string> program, Context context)
        {
            var node = program;
            node.index = calculateIndex(node);
            if (!node.IsHole)
                node.expression = context.MkBoolConst($"C_{node.index}_{node.Data.ToString()}");
            foreach (var child in node.Children)
                {
                    generateIndexes(child, context);
                }
        }
        public void Propogate(TreeNode<string> root, Lemmas lemmas, Context context, Grammar grammar)
        {
            var satEncodedProgram = SATEncoder<string>.SATEncodeTempLight(root, context);
            var lemmasIConjunction = lemmas.LemmasInConjunction(context);

            var rule1 = grammar.productions.First();

            var holes = root.Holes();

            foreach(var hole in holes)
            {
                var index = hole.Parent.Children.IndexOf(hole);
                var nonTerminalToExpand = hole.Parent.rule.rightHandSide[index + 1];
                var rules = grammar.productions.Where(x => x.leftHandSide == nonTerminalToExpand).ToList();

               
                var componentsFromRules_SATEncoded = rules.Select(x => context.MkBoolConst($"C_{hole.index}_{x.component}"));
                var or_componentsFromRules_SATEncoded = context.MkOr(componentsFromRules_SATEncoded);

                foreach (var rule in rules)
                {
                    var component_SATEncoded = context.MkBoolConst($"C_{hole.index}_{rule.component}");

                    var leftHandSide = context.MkAnd(new BoolExpr[] { satEncodedProgram, lemmasIConjunction, or_componentsFromRules_SATEncoded });
                    var check = context.MkNot(context.MkImplies(leftHandSide, component_SATEncoded));
                    if (SMTSolver.CheckIfUnSAT(context, check))
                    {
                        hole.FillHole(rule.component, rule);
                        Propogate(root, lemmas, context, grammar);
                    }
                        
                }                
            }          
        }
            public TreeNode<string> AssignRandomly(TreeNode<string> currentNode, Lemmas lemmas, Context context, Grammar grammar)
        {
            
            currentNode = generateRandomAssignment_AST(currentNode, lemmas, context, grammar);
            //generateIndexes(currentNode, context);

            return currentNode;
        }

        public TreeNode<string> generateRandomProgram()
        {
            var program = new TreeNode<string>(startSymbol);
            generateRandomAssignments_AST(program);
            //generateIndexes(program, context);

            return program;
        }    
        private void generateRandomAssignments(TreeNode<string> currentNode)
        {
            var currentLeftHandSide = currentNode.Data;

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();
            var index = rand.Next(0, (possibleProductionRules.Count()));
            var choosenProductionRule = possibleProductionRules.ElementAt(index);

            foreach (var rhs in choosenProductionRule.rightHandSide)
            {
                if (IsTerminal(rhs))
                {
                    var newChildNode = currentNode.AddChild(rhs, choosenProductionRule.arity);
                    currentNode = newChildNode;
                }
                    if (IsNonTerminal(rhs))
                {
                    var newChildNode = currentNode.AddChild(rhs, choosenProductionRule.arity);
                    generateRandomAssignments(newChildNode);
                }
            }
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

        private TreeNode<string> FindHole_DFS(TreeNode<string> root)
        {
            var stack = new Stack<TreeNode<string>>();
            stack.Push(root);

            while(stack.Count() != 0)
            {
                var current = stack.Pop();
                if(current.IsHole)
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

        private TreeNode<string> generateRandomAssignment_AST(TreeNode<string> root, Lemmas lemmas, Context context, Grammar grammar)
        {
            var hole = FindHole_DFS(root);

            var condition = root.holes == null;
            var currentLeftHandSide = condition ? grammar.startSymbol : hole.Parent.holes.Pop();

            if(!condition)
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

                holeToFill.FillHole(terminal, choosenProductionRule);

                generateIndexes(hole, context);
                if (RuleResultsInLeaf(grammar, choosenProductionRule))
                {
                    var satEncodedProgram = SATEncoder<string>.SATEncodeTempLight(root, context);
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
                        productions.Remove(holeToFill.rule);

                    return hole;
                }
            }
            return null;
        }

        public bool RuleResultsInLeaf(Grammar grammar, Production rule)
        {
            var check =(grammar.nonTerminals.Where(x => rule.rightHandSide.Contains(x))?.Count() == 0);
            return check;
        }

        private TreeNode<string> generateRandomAssignment_AST_WithPropogate(TreeNode<string> currentNode, Lemmas lemmas, List<Tuple<string, string>> z3ComponentSpecs, Context context, Grammar grammar)
        {
            var currentLeftHandSide = currentNode.holes == null ? "N" : currentNode.holes.Pop();

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();

            var root = currentNode;

            while (possibleProductionRules.Count > 0)
            {
                while (root.Parent != null)
                    root = currentNode.Parent;

                //var index = rand.Next(0, (possibleProductionRules.Count()));
                var index = 0;
                var choosenProductionRule = possibleProductionRules.ElementAt(index);
                
                var terminal = choosenProductionRule.rightHandSide.First();

                var holeToFill = currentNode.IsHole ? currentNode : currentNode.Children.FirstOrDefault(x => x.IsHole);

                holeToFill.FillHole(terminal, choosenProductionRule);
                generateIndexes(root, context);

                
                if (!RuleResultsInLeaf(grammar, choosenProductionRule))
                {
                    var satEncodedProgram = SATEncoder<string>.SATEncodeTempLight(root, context);
                    foreach (var lemma in lemmas)
                    {
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
                else
                {
                    return generateRandomAssignment_AST_WithPropogate(holeToFill, lemmas, z3ComponentSpecs, context, grammar);
                }

                if (!holeToFill.IsHole)
                {                        
                    return holeToFill;
                }
            }
            return null;
        }

        private TreeNode<string> generateRandomAssignment_AST2(TreeNode<string> currentNode, string lhs = "N")
        {

            var currentLeftHandSide = lhs;

            if (currentNode.holes?.Count() == 0 || currentNode.holes == null)
            {
                var possibleProductionRules1 = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();
                var index1 = rand.Next(0, (possibleProductionRules1.Count()));
                var choosenProductionRule1 = possibleProductionRules1.ElementAt(index1);

                var terminal1 = choosenProductionRule1.rightHandSide.First();

                var newChildNode1 = currentNode.AddChild(terminal1, choosenProductionRule1.arity);
                currentNode = newChildNode1;
                currentNode.holes = new Stack<string>(choosenProductionRule1.rightHandSide.GetRange(1, choosenProductionRule1.rightHandSide.Count() - 1));
                return currentNode;
            }

            currentLeftHandSide = currentNode.holes.Pop();

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();
            var index = rand.Next(0, (possibleProductionRules.Count()));
            var choosenProductionRule = possibleProductionRules.ElementAt(index);

            var terminal = choosenProductionRule.rightHandSide.First();

            var newChildNode = currentNode.AddChild(terminal, choosenProductionRule.arity);
            currentNode = newChildNode;
            currentNode.holes = new Stack<string>(choosenProductionRule.rightHandSide.GetRange(1, choosenProductionRule.rightHandSide.Count() - 1));

            return currentNode;
        }

        private void generateRandomAssignments_AST(TreeNode<string> currentNode, string lhs = "N")
        {
            var currentLeftHandSide = lhs;

            var possibleProductionRules = productions.Where(x => x.leftHandSide == currentLeftHandSide).ToList();
            var index = rand.Next(0, (possibleProductionRules.Count()));
            var choosenProductionRule = possibleProductionRules.ElementAt(index);

            foreach (var rhs in choosenProductionRule.rightHandSide)
            {
                if (IsTerminal(rhs))
                {
                    var newChildNode = currentNode.AddChild(rhs, choosenProductionRule.arity);
                    currentNode = newChildNode;
                }
                if(IsNonTerminal(rhs))
                {
                    generateRandomAssignments_AST(currentNode, rhs);
                }
            }
        }

        private bool IsTerminal(string rhs)
        {
            return terminals.Contains(rhs);
        }
        private bool IsNonTerminal(string rhs)
        {
            return nonTerminals.Contains(rhs);
        }
    }
}