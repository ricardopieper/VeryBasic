using System.Collections.Generic;
using System;
using System.Linq;
namespace VeryBasic.Parser.Nodes
{
    public class Expression : Node
    {
        private readonly List<object> postfix;
        public Expression(List<object> postfix) { this.postfix = postfix; } 
            
        public override string ToString() => string.Join(" ", postfix.Select(x=>x.ToString()));
    }
}