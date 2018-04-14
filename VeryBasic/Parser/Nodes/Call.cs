namespace VeryBasic.Parser.Nodes
{
    public class Call : Node
    {
        private readonly string functionName;
        private ExpressionList callArgs;
        public Call(string functionName)
        {
            this.functionName = functionName;
        }
        
        public Call(string functionName, ExpressionList callArgs)
        {
            this.functionName = functionName;
            this.callArgs = callArgs;
        }

        public override string ToString() => functionName+"("+(callArgs == null? "" : callArgs.ToString())+")";
    }
}