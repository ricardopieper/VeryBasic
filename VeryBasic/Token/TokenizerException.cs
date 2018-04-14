using System;

namespace VeryBasic.Token
{
    public class TokenizerException : Exception
    {
        public static string GetSourceCode(Tokenizer tokenizer)
        {
            int interval = 10;

            int start = tokenizer.Position - interval;
            int end = tokenizer.Position + interval;
            if (start < 0) start = 0;

            return tokenizer.Source.Substring(start, tokenizer.Position)
                      + "[" + tokenizer.Peek() + "]"
                 + tokenizer.Source.Substring(tokenizer.Position + 1, end - interval);

        }
        public TokenizerException(string message, Tokenizer tokenizer) : base(message + ", tokenizer location: " + GetSourceCode(tokenizer)) { }
    }

    public class TokenizerFatalException : Exception
    {
        public TokenizerFatalException(string message, Tokenizer tokenizer) 
            : base("TOKENIZER FATAL ERROR: "+message + ", tokenizer state: " + TokenizerException.GetSourceCode(tokenizer)) { }
    }
}