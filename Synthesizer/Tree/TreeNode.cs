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
        public ICollection<TreeNode<T>> Children { get; set; }
        public int index;
        public int arity;
        public Production rule;
        public Stack<string> holes;
        public BoolExpr expression;

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

        //public Boolean IsLeaf
        //{
        //    get { return Children.Count(x => x.Data != null) != Children.Count(); }
        //}

        public int Level
        {
            get
            {
                if (this.IsRoot)
                    return 1;
                return Parent.Level + 1;
            }
        }

        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();
            //this.Children = new LinkedList<TreeNode<T>>() { new TreeNode<T>() };
            //this.Children.Add(new TreeNode<T>());
            //this.holes = new Stack<string>(new List<string>() { "N" });           
            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }
        public TreeNode()
        {
            this.Children = new LinkedList<TreeNode<T>>();

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
        }

        public void FillHole(T child, Production rule)
        {
            //var childNode = this.Children.FirstOrDefault(x => x.IsHole);
            //childNode.Data = child;
            var times = rule.rightHandSide.Count() - 1;
            this.Data = child;

            this.rule = rule;
            this.holes = new Stack<string>(rule.rightHandSide.GetRange(1, rule.rightHandSide.Count() - 1));

            times.Times(() =>
            {
                this.AddChild();
            });
            this.RegisterChildForSearch(this);
            //return this;
        }

        public TreeNode<T> AddChild(T child, int arity)
        {
            TreeNode<T> childNode = new TreeNode<T>(child) { Parent = this, arity = arity };
            this.Children.Add(childNode);

            //this.RegisterChildForSearch(childNode);

            return childNode;
        }
        public TreeNode<T> AddChild()
        {
            TreeNode<T> childNode = new TreeNode<T>() { Parent = this };
            this.Children.Add(childNode);

            //this.RegisterChildForSearch(childNode);

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