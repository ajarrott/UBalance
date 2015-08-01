﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UBalance.Library.Classes
{
    public class Calculation
    {
        private double _equatedValue;
        private string _exp;
        private static Dictionary<string, double> _dict;
        private Node _root;
 

        public Calculation(string s)
        {
            // remove white spaces
            _dict = new Dictionary<string, double>();
            _exp = s.Replace(" ", String.Empty);
            _equatedValue = SetExp(_exp);
        }

        public class Node
        {

        }

        // always a child node
        private class ChildNode : Node
        {
            private double _val;

            public ChildNode(double d)
            {
                _val = d;
            }
            public double getVal()
            {
                return _val;
            }
        }

        // always a child node
        private class VarNode : Node
        {
            private string _val;

            public VarNode(string s)
            {
                _val = s;
            }
            public string getVal()
            {
                return _val;
            }
        }

        // can have children nodes
        private class OperatorNode : Node
        {
            private char _val;

            // main value constructor
            public OperatorNode(char op)
            {
                _val = op;
            }

            // Left node constructor
            public Node Left { get; set; }

            // Right node constructor
            public Node Right { get; set; }

            // evaluates the expression
            public double Eval(double a, double b)
            {
                switch (_val)
                {
                    case '+':
                        return a + b;
                    case '-':
                        return a - b;
                    case '*':
                        return a * b;
                    case '/':
                        if (b == 0)
                        {
                            //Console.WriteLine("cannot divide by 0, returning 0");
                            return 0;
                        }
                        return a / b;
                    case '^':
                        return Math.Pow(a, b);
                    default:
                        return 0;
                }
            }
        }

        // set the expression string
        public double SetExp(string s)
        {
            _root = MakeTree(_exp); // make the tree based off the of the users expression
            return Evaluate();
        }

        public double ReEval()
        {
            return Evaluate();
        }

        // recursively makes the tree based on the current string and operator
        // nothing but valid operators should be sent to this function
        private static Node MakeTree(string s, char op)
        {
            int pcounter = 0; // parenthesis counter

            for (int i = s.Length - 1; i >= 0; i--) // 0 based indexing, start from right of the string
            {
                // keep count of total parentesis open and closed
                if (')' == s[i]) // check if closing parenthesis
                    pcounter++;
                else if ('(' == s[i]) // check for open parenthesis
                    pcounter--;

                // no mismatching parenthesis set op to a valid op, do not want to set parenthesis to opnode
                if (0 == pcounter && op == s[i])
                {
                    OperatorNode on = new OperatorNode(s[i]); // make a new opnode
                    on.Left = MakeTree(s.Substring(0, i)); // make left subtree
                    on.Right = MakeTree(s.Substring(i + 1)); // make right subtree
                    return on;
                }
            }

            if (pcounter != 0) // no matching parenthesis
            {
                if (pcounter < 0)
                    throw new Exception("Missing 1 or more closing parenthesis");
                else
                    throw new Exception("missing 1 or more opening parenthesis");
            }

            return null; // no node created
        }
        
        // make tree based off of the current string
        private static Node MakeTree(string s)
        {
            if (string.IsNullOrEmpty(s)) // no string, return no node
                return null;

            // get rid of parenthesis
            if ('(' == s[0])
            {
                int counter = 0;
                for (int i = 1; i < s.Length; i++) // check for other parenthesis
                {
                    if ('(' == s[i])
                    {
                        counter++;
                    }
                    else if (')' == s[i])
                    {
                        counter--;
                        if (0 == counter) // send items inside parenthesis for current string
                        {
                            // not last char
                            if (i != s.Length - 1)
                            {
                                break;
                            }
                            else if (s[0] == '(')
                            {
                                //make tree again ( based off the stringwithout outside parenthesis)
                                return MakeTree(s.Substring(1, s.Length - 2));
                            }
                            else
                                break; // otherwise does not need to create a substring again, go operator loop
                        }
                    }
                    // make tree again (based off the string without outside parenthesis)
                    if (')' == s[s.Length - 1] && s[0] == '(')
                    {
                        s = s.Substring(1, s.Length - 2);
                        //counter = 0;  // reset counter to 0 since parenthesis removed;
                    }
                }
            }
            // valid op characters in reverse PEMDAS order
            char[] ops = { '+', '-', '*', '/', '^' };

            //send the string to search for each valid operator allowed
            foreach (char op in ops)
            {
                Node n = MakeTree(s, op); // make the tree based off the current operator
                if (n != null) return n; // if a node was returned, return that node to the calling function
            }

            double num; // try to make the string as a value
            if (double.TryParse(s, out num)) // if it works
            {
                return new ChildNode(num); // return a cNode (constant node)
            }
            else
            {
                return MakeVarNode(s); // otherwise it has letters so make a variable node
            }
        }

        // public evaluate function
        private double Evaluate()
        {
            // evaluates the tree based on the root node
            return EvalTree(_root);
        }

        // evaluate the tree recursively
        private double EvalTree(Node n)
        {
            if (n == null)
            {
                //Console.WriteLine("No expression entered.");
                return 0;
            }
            else
            {
                if (n is ChildNode) // tree only has one const node
                {
                    ChildNode n1 = (ChildNode)n;
                    return n1.getVal();

                }
                else if (n is VarNode) // tree has one variable node
                {
                    VarNode n2 = (VarNode)n;
                    return _dict[n2.getVal()]; // return value assigned by user in the dictionary
                }
                else // m_root is a opNode
                {
                    // need to evaluate the tree
                    double l;
                    double r;
                    OperatorNode op = (OperatorNode)n;

                    l = EvalTree(op.Left);
                    r = EvalTree(op.Right);
                    return op.Eval(l, r);
                }
            }
        }

        // set variable
        public void SetVar(string key, string value)
        {
            // make sure it's a value
            double d = 0;
            if ( double.TryParse(value, out d))
            {
                _dict[key] = Convert.ToDouble(value); // get number from user
            }
            // otherwise set the value to 0
            else
            {
                _dict[key] = 0;
            }
        }

        // make var node with value of 0
        private static Node MakeVarNode(string s)
        {
            // make sure varnode is of type (starts with a character) then has letters and numbers)
            if (Regex.IsMatch(s, "^[a-zA-Z]+[a-zA-Z0-9]*") == true)
            {
                if (_dict.ContainsKey(s))
                {
                    return new VarNode(s);
                }
                else
                {
                    // if user doesn't update the value it will be 0
                    _dict.Add(s, 0);
                    return new VarNode(s);
                }

            }
            return null;
        }

        // return the expression, for checking in the spreadsheet
        public string getExp
        {
            get
            {
                return _exp;
            }
        }

    }
}
