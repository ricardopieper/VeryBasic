namespace VeryBasic.Token
{
    public class TokenLiteralNumber : TokenLiteral
    {
        public TokenLiteralNumber(double value) => Value = value;
        public double Value { get; set; }

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}