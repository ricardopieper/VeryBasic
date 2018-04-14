using System.Collections.Generic;
using System;
using System.Linq;

namespace VeryBasic.Parser.Nodes
{
    public class ExpressionList : Node
    {
        private readonly List<Expression> expressions;
        public ExpressionList(List<Expression> expressions) { this.expressions = expressions; }
          

        public override string ToString(){
            return string.Join(", ", expressions.Select(x=>x.ToString()));
        }
    }
}