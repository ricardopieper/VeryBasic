using System.Collections.Generic;
using System;
namespace VeryBasic.Token
{
    public class TokenIdentifier : BaseToken
    {
        private static Dictionary<string, Func<BaseToken>> factories = new Dictionary<string, Func<BaseToken>>();

        static TokenIdentifier()
        {
            factories.Add("if", () => new TokenIf());
            factories.Add("else", () => new TokenElse());
            factories.Add("endif", () => new TokenEndIf());
            factories.Add("while", () => new TokenWhile());
            factories.Add("endwhile", () => new TokenEndWhile());
            factories.Add("true", () => new TokenBooleanLiteralTrue());
            factories.Add("false", () => new TokenBooleanLiteralFalse());
        }

        public TokenIdentifier(string identifier)
        {
            this.Identifier = identifier;
        }

        public string Identifier { get; set; }
        public override string ToString() => Identifier;

        public BaseToken GetKeywordOrIdentifier()
        {
            if (factories.TryGetValue(Identifier, out var tokenFactory))
            {
                return tokenFactory();
            }
            else
            {
                return this;
            }
        }

    }
}