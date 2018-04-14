using System.Collections.Generic;

namespace VeryBasic.Parser.Nodes
{
    public class While : Node
    {
        public Expression _expr;
        public List<Node> _statements;

        public While(Expression expr, List<Node> statements){
            _expr = expr;
            _statements = statements;
        }
    }
}