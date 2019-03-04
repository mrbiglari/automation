using CSharpTree;
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

        private Random rand = new Random(1);
        //private Random rand = new Random(5);


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
        public Grammar(string startSymbol, List<string> nonTerminals, List<string> terminals, List<Production> productions, int maxArity)
        {
            this.startSymbol = startSymbol;
            this.nonTerminals = nonTerminals;
            this.terminals = terminals;
            this.productions = productions;
            this.maxArity = maxArity;
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

        public void generateIndexes(TreeNode<string> program)
        {
            var node = program;
            node.index = calculateIndex(node);
            foreach (var child in node.Children)
                {
                    generateIndexes(child);
                }
        }

        public TreeNode<string> generateRandomAssignment(TreeNode<string> currentNode)
        {
            
            currentNode = generateRandomAssignment_AST(currentNode);
            generateIndexes(currentNode);

            return currentNode;
        }

        public TreeNode<string> generateRandomProgram()
        {
            var program = new TreeNode<string>(startSymbol);
            generateRandomAssignments_AST(program);
            generateIndexes(program);

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

        private TreeNode<string> generateRandomAssignment_AST(TreeNode<string> currentNode, string lhs = "N")
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