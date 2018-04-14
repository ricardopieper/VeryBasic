using VeryBasic.Token;

namespace VeryBasic.Parser.Nodes
{
    public class Attribution : Node
    {
        private readonly TokenIdentifier variableName;
        private readonly Expression variableValue;
        public Attribution(TokenIdentifier variableName, Expression variableValue) 
        {
            this.variableName = variableName;
            this.variableValue = variableValue;
        }

        public override string ToString(){

            return variableName + " = "+ variableValue;

        }
    }
}