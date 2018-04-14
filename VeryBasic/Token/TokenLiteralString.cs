namespace VeryBasic.Token
{
    public class TokenLiteralString : TokenLiteral
    {
        public TokenLiteralString(string value) => Value = value;
        public string Value { get; set; }

        public override string ToString() => Value;
    }
}