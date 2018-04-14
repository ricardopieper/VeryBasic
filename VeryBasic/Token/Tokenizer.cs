using System;
using System.Collections.Generic;
using System.Text;

namespace VeryBasic.Token
{
    public class Tokenizer
    {
        public int Position = 0;
        public string Source;
        public Tokenizer(string source)
        {
            this.Source = source;
        }


        public IEnumerable<BaseToken> Tokenize()
        {

            while (!Eof)
            {
                var chr = Peek();
                if (chr == '"')
                {
                    yield return GetStringLiteral();
                }
                else if (char.IsNumber(chr))
                {
                    yield return GetNumberLiteral();
                }
                else if (char.IsLetter(chr) || chr == '_')
                {
                    yield return GetUnquotedIdentifier();
                }
                else if (chr == '=')
                {
                    chr = ReadNext();
                    if (chr == '=')
                    {
                        chr = ReadNext();
                        yield return new TokenEquals();
                    }
                    else
                    {
                        yield return new TokenAttribution();
                    }
                }
                else if (chr == '&')
                {
                    chr = ReadNext();
                    if (chr == '&')
                    {
                        chr = ReadNext();
                        yield return new TokenAnd();
                    }
                    else
                    {
                        if (!IsTrivia())
                            throw new TokenizerException("unexpected " + Peek() + ", AND operator not available", this);
                    }
                }
                else if (chr == '(')
                {
                    chr = ReadNext();
                    yield return new TokenOpenParen();
                }
                else if (chr == ')')
                {
                    chr = ReadNext();
                    yield return new TokenCloseParen();
                } 
                else if (chr == ',')
                {
                    chr = ReadNext();
                    yield return new TokenComma();
                }
                else if (chr == '/')
                {
                    chr = ReadNext();
                    yield return new TokenDivide();
                }
                else if (chr == '*')
                {
                    chr = ReadNext();
                    yield return new TokenMultiply();
                }
                else if (chr== '+')
                {
                    chr = ReadNext();
                    yield return new TokenPlus();
                }
                else if (chr == '-')
                {
                    chr = ReadNext();
                    yield return new TokenMinus();
                }
                else if (chr == '>')
                {
                    chr = ReadNext();
                    if (chr == '=')
                    {
                        chr = ReadNext();
                        yield return new TokenGreaterOrEqualsThan();
                    }
                    else
                    {
                        yield return new TokenGreaterThan();
                    }
                }
                else if (chr == '<')
                {
                    chr = ReadNext();
                    if (chr == '=')
                    {
                        chr = ReadNext();
                        yield return new TokenLowerOrEqualsThan();
                    }
                    else
                    {
                        yield return new TokenLowerThan();
                    }
                }
                else if (chr == '!')
                {
                    chr = ReadNext();
                    if (chr == '=')
                    {
                        chr = ReadNext();
                        yield return new TokenNotEquals();
                    }
                    else
                    {
                        yield return new TokenNot();
                    }
                }
                else if (chr == '|')
                {
                    chr = ReadNext();
                    if (chr == '|')
                    {
                        chr = ReadNext();
                        yield return new TokenOr();
                    }
                    else
                    {
                        if (!IsTrivia())
                            throw new TokenizerException("unexpected " + Peek() + ", AND operator not available", this);
                    }
                }
                else if (chr == '\r')
                {
                    chr = ReadNext();
                    if (chr == '\n')
                    {
                        chr = ReadNext();
                        yield return new TokenNewLine();
                    }
                    else
                    {
                        throw new TokenizerException("unexpected " + Peek() + ", newline expected", this);
                    }
                }
                else if (chr == '\n') yield return new TokenNewLine();
                else throw new TokenizerException("Unexpected character "+Peek(), this);

                SkipTrivia();

            }
        }

        /* 
        public bool Expect<TToken>(string match, out TToken tok) where TToken : Token, new()
        {
            int index = 0;
            char chr = Peek();
            if (match[0] == chr) //se nao der o match no primeiro, nao é sinal de erro
            {
                index++;
                while (index < match.Length - 1)
                {
                    chr = ReadNext();
                    if (match[index++] != chr)
                    {
                        tok = null;
                        throw new TokenizerException("unexpected " + Peek() + ", AND operator not available", this);
                    }
                }
               
                tok = new TToken();
                return true;
            }
            else
            {
                ex = null;
                tok = null;
                return false;
            }

        }*/
        private BaseToken GetUnquotedIdentifier()
        {
            StringBuilder sb = new StringBuilder();
            var chr = Peek();
            if (char.IsLetter(chr) || chr == '_')
            {
                while (!IsTrivia(chr) && (char.IsLetterOrDigit(chr) || chr == '_'))
                {
                    sb.Append(Read());
                    chr = Peek();
                }
            }
            else
            {
                throw new TokenizerFatalException("GetUnquotedIdentifier should be called when Peek() returns a letter or underline [_], currently returning [" + Peek() + "]", this);
            }
            return new TokenIdentifier(sb.ToString()).GetKeywordOrIdentifier();
        }

        public void SkipTrivia()
        {
            while (IsTrivia()) Read();
        }

        public bool IsTrivia() => IsTrivia(Peek());
        public bool IsTrivia(char chr) => chr == ' ' || chr == '\t';

        private TokenLiteralString GetStringLiteral()
        {
            StringBuilder sb = new StringBuilder();
            if (Peek() != '\"')
            {
                throw new TokenizerFatalException("GetStringLiteral should be called when Peek() returns [\"], currently returning [" + Peek() + "]", this);
            }
            ReadNext();

            do
            {
                sb.Append(Peek());
                ReadNext();
            }
            while (Peek() != '"' && !Eol && !Eof); //dont let strings take more than 1 line because newline is a key token

            if (Peek() != '\"')
            {
                throw new TokenizerFatalException("At this stage, GetStringLiteral should end on a [\"], currently returning [" + Peek() + "]", this);
            }

            ReadNext();

            return new TokenLiteralString(sb.ToString());
        }

        private TokenLiteralNumber GetNumberLiteral()
        {
            StringBuilder sb = new StringBuilder();
            bool point = false;

            if (Peek() != '.' && !char.IsNumber(Peek()))
            {
                throw new TokenizerFatalException("GetNumberLiteral should be called when Peek() returns either a number or a point [.], currently returning [" + Peek() + "]", this);
            }

            do
            {
                var chr = Peek();
                if (chr == '.')
                {
                    if (!point) point = true;
                    else throw new TokenizerException("unexpected decimal separator", this);
                }

                sb.Append(chr);

                ReadNext();
            }
            while ((char.IsNumber(Peek()) || Peek() == '.') && !Eof); //no need to check eol, IsNumber will avoid it

            return new TokenLiteralNumber(Convert.ToDouble(sb.ToString(), System.Globalization.CultureInfo.InvariantCulture));
        }

        public char Read() => Eof ? default(char) : Source[Position++];

        public char ReadNext() => Eof ? default(char) : Source[++Position];

        public char Peek() => Source[Position];

        public bool Eof => Position == Source.Length - 1;

        public bool Eol => (Source[Position] == '\n') || (Source[Position] == '\r' || (Eof || Source[Position + 1] == '\n'));
    }
}