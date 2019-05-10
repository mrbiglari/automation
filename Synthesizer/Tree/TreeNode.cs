using Microsoft.Z3;
using Synthesis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSharpTree
{
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Data { get; set; }
        public string Spec;
        public TreeNode<T> Parent { get; set; }
        public List<TreeNode<T>> Children { get; set; }
        public int index;
        public int arity;
        public Production rule;
        public Stack<string> holes;
        public Stack<string> holesBackTrack;
        public BoolExpr expression;
        private int rootLevel;

        public List<TreeNode<T>> Holes(List<TreeNode<T>> holes = null)
        {
            if (holes == null)
                holes = new List<TreeNode<T>>();
            foreach(var child in Children)
            {
                if (child.IsHole)
                    holes.Add(child);
                child.Holes(holes);
            }
            return holes;
        }

        public Boolean IsHole
        {
            get { return Children.Count == 0 && Data == null; }
        }

        public Boolean IsRoot
        {
            get { return Parent == null; }
        }

        public Boolean IsLeaf
        {
            get { return Children.Count == 0 && Data != null; }
        }

        public Boolean IsConcrete
        {
            get { return !ContainsHoles(); }
        }
        public bool ContainsHoles()
        {
            var isHole = IsHole;
            foreach (var child in Children)
            {
                isHole = isHole || child.ContainsHoles();
            }
            return isHole;
        }

        public int Level
        {
            get
            {
                if (this.IsRoot)
                    return rootLevel;
                return Parent.Level + 1;
            }
        }

        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new List<TreeNode<T>>();     
            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }
        public TreeNode(int rootLevel = 1)
        {
            this.Children = new List<TreeNode<T>>();
            this.rootLevel = rootLevel;
            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }

        public TreeNode<T> GetAtIndex(int index)
        {
            if (this.index == index)
                return this;
            else
            {
                foreach(var child in this.Children)
                {
                    return child.GetAtIndex(index);
                }
            }
            return null;
        }

        public void MakeHole()
        {
            this.Data = default(T) ;

            this.rule = null;

            Children = new List<TreeNode<T>>();
            holes = new Stack<string>();
            holesBackTrack = new Stack<string>();
        }

        public int calculateIndex(TreeNode<T> node, int maxArity)
        {
            int index = 0;
            if (node.Parent != null)
            {
                var parentPositionInRow = ((node.Parent.index) * maxArity);
                var currentNodePositionInParentsChildrenList = (node.Parent.Children.ToList().IndexOf(node));
                index = currentNodePositionInParentsChildrenList + parentPositionInRow + 1;
            }
            return index;
        }
        public void generateIndexes(TreeNode<T> program, Context context, int maxArity)
        {
            var node = program;
            node.index = calculateIndex(node, maxArity);
            //if (!node.IsHole)
            //    node.expression = context.MkBoolConst($"C_{node.index}_{node.Data.ToString()}");
            foreach (var child in node.Children)
            {
                generateIndexes(child, context, maxArity);
            }
        }

        public void FillHole(T componentName, Production rule, Context context, Grammar grammar, int index = 0)
        {
            var times = rule.rightHandSide.Count() - 1;
            this.Data = componentName;

            this.rule = rule;
            var holesAsList = rule.rightHandSide.GetRange(1, rule.rightHandSide.Count() - 1);
            holesAsList.Reverse();
            this.holes = new Stack<string>(holesAsList);
            this.holesBackTrack = new Stack<string>();            
            this.index = (index == 0) ? calculateIndex(this, grammar.maxArity) : index;
            this.expression = context.MkBoolConst($"C_{this.index}_{Data.ToString()}");
            times.Times(() =>
            {
                this.AddChild();
                Children.Last().index = calculateIndex(Children.Last(), grammar.maxArity);                
            });
            this.RegisterChildForSearch(this);            
        }

        public TreeNode<T> AddChild(T child, int arity)
        {
            TreeNode<T> childNode = new TreeNode<T>(child) { Parent = this, arity = arity };
            this.Children.Add(childNode);
            return childNode;
        }
        public TreeNode<T> AddChild()
        {
            TreeNode<T> childNode = new TreeNode<T>() { Parent = this, rootLevel = rootLevel };
            Children.Add(childNode);

            return childNode;
        }

        public void RemoveElement(TreeNode<T> root, T data)
        {
            var elementToDelete = root.Where(x => Comparer<T>.Default.Compare(x.Data, data) >= 0).FirstOrDefault();

            elementToDelete.Parent.Children = elementToDelete.Children;
            foreach (var element in elementToDelete.Children)
            {
                element.Parent = elementToDelete.Parent;
            }
        }

        public TreeNode<T> ChipRoot()
        {
            this.Children.First().Parent = null;
            return this.Children.First();
        }

        public void Visualize()
        {
            TreeVisualizer<T>.PrintNodes(this, "");
        }

        public override string ToString()
        {
            return Data != null ? Data.ToString() : "[data null]";
        }

        #region searching

        public ICollection<TreeNode<T>> ElementsIndex { get; set; }

        private void RegisterChildForSearch(TreeNode<T> node)
        {
            ElementsIndex.Add(node);
            if (Parent != null)
                Parent.RegisterChildForSearch(node);
        }

        public TreeNode<T> FindTreeNode(Func<TreeNode<T>, bool> predicate)
        {
            return this.ElementsIndex.FirstOrDefault(predicate);
        }

        #endregion

        #region iterating

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            yield return this;
            foreach (var directChild in this.Children)
            {
                foreach (var anyChild in directChild)
                    yield return anyChild;
            }
        }

        #endregion
    }
}