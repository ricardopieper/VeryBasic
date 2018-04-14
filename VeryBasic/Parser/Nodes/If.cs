using System.Collections.Generic;

namespace VeryBasic.Parser.Nodes
{
    public class If : Node
    {

        public Expression expr;
        public List<Node> trueStatements;
        public List<Node> falseStatements;

        public If(Expression expr, List<Node> trueStatements, List<Node> falseStatements){
            this.expr = expr;
            this.trueStatements = trueStatements;
            this.falseStatements = falseStatements;
        }
    }
}