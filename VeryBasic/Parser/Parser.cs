using System.Collections.Generic;
using System.Linq;
using VeryBasic.Token;
using VeryBasic.Parser.Nodes;
using System;
using System.Collections;

namespace VeryBasic.Parser
{

    //What i'm trying to achieve here is a way to go all the way back to a certain point if I eventually try to parse a thing optionally and it fails.
    //I should be able to create checkpoints and restore to that point. The TokenSource points to the farthest point the parser has reached yet,
    //while the state of the LinkedList is what the parser is truly doing. The inexistence of items inside the TokenSource indicates that
    //the checkpoint functionality has not been used yet. Therefore, the checkpoint should be only used when CREATING and RESTORING a checkpoint, in order to 
    //use a small amount of memory.
    public class TokenList : IEnumerator<BaseToken>
    {
        public TokenList(IEnumerator<BaseToken> tokenSource)
        {
            this.TokenSource = tokenSource;
        }

        public IEnumerator<BaseToken> TokenSource;

        public BaseToken Current
        {
            get
            {
                if (Tokens.Count > 0 && !RecordingCheckpoint) return Tokens.First.Value;
                else return TokenSource.Current;
            }
        }

        object IEnumerator.Current => this.Current;

        public LinkedList<BaseToken> Tokens = new LinkedList<BaseToken>();

        private bool RecordingCheckpoint = false;

        public void CreateCheckpoint()
        {
            if (RecordingCheckpoint || Tokens.Count > 0)
                throw new NotSupportedException("Cannot have multiple checkpoints, must call ResetCheckpoint and must consume all tokens");

            Tokens.AddLast(TokenSource.Current);
            RecordingCheckpoint = true;
        }

        public void ResetCheckpoint()
        {
            RecordingCheckpoint = false;
        }

        public void ConfirmCheckpoint()
        {
            RecordingCheckpoint = false;
            Tokens = new LinkedList<BaseToken>();
        }


        public void Dispose()
        {
            TokenSource.Dispose();
        }

        public bool MoveNext()
        {
            if (RecordingCheckpoint) //if it is recording a checkpoint, just copy the value to the Tokens linked list
            {
                bool moveNext = TokenSource.MoveNext();

                var cur = TokenSource.Current;

                Tokens.AddLast(cur);

                return moveNext;
            }
            else //if its not recording, should consume the tokens cached in the Tokens linked list before proceeding consuming the tokensource
            {
                if (Tokens.Count > 0)
                {
                    var cur = Tokens.First;
                    Tokens.Remove(cur);

                    if (Tokens.Count == 0)
                    {
                        return TokenSource.MoveNext();
                    }
                    else return true;
                }
                else
                {
                    return TokenSource.MoveNext();
                }
            }
        }

        public void Reset()
        {
            TokenSource.Reset();
        }
    }

    public class Parser
    {
        private readonly TokenList EnumeratorTokens;

        public Parser(IEnumerable<BaseToken> tokens)
        {
            this.EnumeratorTokens = new TokenList(tokens.GetEnumerator());
            this.EnumeratorTokens.MoveNext();
        }


        public IEnumerable<Node> Parse()
        {
            foreach (var node in ParseStatements())
            {
                Console.WriteLine(node);
                yield return node;
            }

        }

        public List<Node> ParseStatements()
        {
            List<Node> parsedNodes = new List<Node>();
            while (!(Peek() is TokenEOF) && Peek() != null)
            {
                var current = Peek();
                //top-level parsing
                if (current is TokenIdentifier identifier)
                {
                    Node syntaxNode = null;

                    EnumeratorTokens.CreateCheckpoint();

                    syntaxNode = ParseStatementLevelFunctionCall(identifier);

                    if (syntaxNode != null) EnumeratorTokens.ConfirmCheckpoint();
                    else
                    {
                        EnumeratorTokens.ResetCheckpoint();
                        syntaxNode = ParseAttribution(identifier);
                    }

                    if (syntaxNode == null)
                    {
                        throw new ParseException("Expected a function call or an attribution statement.", this);
                    }
                    parsedNodes.Add(syntaxNode);

                    if (Peek() is TokenNewLine)
                    {
                        ReadNext();
                    }
                    else
                    {
                        throw new ParseFatalException("newline expected", this);
                    }

                }
                else if (current is TokenIf tokenIf)
                {
                    var ifNode = ParseIf(tokenIf);
                    if (ifNode == null) throw new ParseException("Expected an if statement", this);
                    parsedNodes.Add(ifNode);
                }
                else if (current is TokenWhile tokenWhile)
                {
                    var whileNode = ParseWhile(tokenWhile);
                    if (whileNode == null) throw new ParseException("Expected a while statement", this);
                    parsedNodes.Add(whileNode);
                }
                else if (current is TokenNewLine || current is TokenEOF || current == null)
                {
                    parsedNodes.Add(new NoOp());
                    if (current is TokenNewLine)
                    {
                        ReadNext();
                    }
                }
                else
                {
                    throw new ParseFatalException("Should be a newline/eof here", this);
                }
            }
            return parsedNodes;
        }

        public List<Node> ParseSingle()
        {
            List<Node> parsedNodes = new List<Node>();

            var current = Peek();
            //top-level parsing
            if (current is TokenIdentifier identifier)
            {
                Node syntaxNode = null;

                EnumeratorTokens.CreateCheckpoint();

                syntaxNode = ParseStatementLevelFunctionCall(identifier);

                if (syntaxNode != null) EnumeratorTokens.ConfirmCheckpoint();
                else
                {
                    EnumeratorTokens.ResetCheckpoint();
                    syntaxNode = ParseAttribution(identifier);
                }

                if (syntaxNode == null)
                {
                    throw new ParseException("Expected a function call or an attribution statement.", this);
                }
                parsedNodes.Add(syntaxNode);

                if (Peek() is TokenNewLine)
                {
                    ReadNext();
                }
                else
                {
                    throw new ParseFatalException("newline expected", this);
                }

            }
            else if (current is TokenIf tokenIf)
            {
                var ifNode = ParseIf(tokenIf);
                if (ifNode == null) throw new ParseException("Expected an if statement", this);
                parsedNodes.Add(ifNode);
            }
            else if (current is TokenWhile tokenWhile)
            {
                var whileNode = ParseWhile(tokenWhile);
                if (whileNode == null) throw new ParseException("Expected a while statement", this);
                parsedNodes.Add(whileNode);
            }
            else if (current is TokenNewLine || current is TokenEOF)
            {
                parsedNodes.Add(new NoOp());
                if (current is TokenNewLine)
                {
                    ReadNext();
                }
            }
            else
            {
                throw new ParseFatalException("Should be a newline/eof here", this);
            }

            return parsedNodes;
        }


        public If ParseIf(TokenIf tokenIf)
        {
            //current is TokenIf
            //next must be Expression, and then NewLine
            ReadNext();
            var expression = ParseExpression(isInFunction: false);
            if (expression == null) throw new ParseException("Expected an expression", this);

            if (Peek() is TokenNewLine)
            {
                ReadNext();

                List<Node> trueStatements = new List<Node>();
                List<Node> falseStatements = new List<Node>();

                if (!(Peek() is TokenElse) && !(Peek() is TokenEndIf))
                {
                    List<Node> statements;
                    while ((statements = ParseSingle()).Count > 0)
                    {

                        foreach (var statement in statements)
                        {
                            trueStatements.Add(statement);

                        }
                        if (Peek() is TokenElse || Peek() is TokenEndIf) break;
                    }

                }


                //have all statements inside the true section parsed, check whether there is an else or if we have to finish it here
                //do not check a newline, the ParseStatements should go past that and stop on TokenElse, TokenEndIf, TokenEndWhile and TokenEOF

                if (Peek() is TokenElse)
                {
                    ReadNext();
                    if (!(Peek() is TokenEndIf))
                    {
                        List<Node> statements;
                        while ((statements = ParseSingle()).Count > 0)
                        {

                            foreach (var statement in statements)
                            {
                                falseStatements.Add(statement);

                            }
                            if (Peek() is TokenEndIf) break;
                        }

                    }
                }

                if (Peek() is TokenEndIf)
                {
                    ReadNext();
                    If ifStatement = new If(expression, trueStatements, falseStatements);
                    return ifStatement;
                }
                else
                {
                    throw new ParseException("Expected endif", this);
                }
            }
            else
            {
                throw new ParseException("Expected new line", this);
            }
        }

        public While ParseWhile(TokenWhile tokenWhile)
        {
            //current is TokenWhile
            //next must be Expression, and then NewLine
            ReadNext();
            var expression = ParseExpression(isInFunction: false);
            if (expression == null) throw new ParseException("Expected an expression", this);

            if (Peek() is TokenNewLine)
            {
                ReadNext();
                List<Node> statements = new List<Node>();

                if (!(Peek() is TokenEndWhile))
                {
                    List<Node> scopeStatements;
                    while ((scopeStatements = ParseSingle()).Count > 0)
                    {
                        foreach (var statement in scopeStatements)
                        {
                            statements.Add(statement);

                        }
                        if (Peek() is TokenEndWhile) break;
                    }
                }

                //have all statements inside the true section parsed, check whether there is an else or if we have to finish it here
                //do not check a newline, the ParseStatements should go past that and stop on TokenElse, TokenEndIf, TokenEndWhile and TokenEOF

                if (Peek() is TokenEndWhile)
                {
                    ReadNext();
                    While whileStatement = new While(expression, statements);
                    return whileStatement;
                }
                else
                {
                    throw new ParseException("Expected endwhile", this);
                }
            }
            else
            {
                throw new ParseException("Expected new line", this);
            }
        }

        public Call ParseStatementLevelFunctionCall(TokenIdentifier functionIdentifier)
        {
            var call = ParseFunctionCall(functionIdentifier);
            if (call == null) return null;
            else
            {
                if (!(Peek() is TokenCloseParen)) throw new ParseFatalException("Parsing a top level function requires that Peek() at this stage returns a TokenCloseParen. It's currently returning (" + Peek().GetType().Name + "). The next step is to determine whether the next token is a TokenEndLine.", this);
                else
                {
                    var tok = ReadNext();
                    if (tok is TokenNewLine) return call;
                    else throw new ParseException("Expected a newline, got " + tok.ToString(), this);
                }
            }
        }

        public Call ParseFunctionCall(TokenIdentifier functionIdentifier)
        {

            if (Peek() != functionIdentifier)
                throw new ParseFatalException($"The current node ({Peek().ToString()}) is not the same as the required ({functionIdentifier.ToString()}). The parser is about to possibly parse a function call, but it cannot determine the function name.", this);

            var next = ReadNext();

            if (next is TokenOpenParen)
            {
                //good chance it will be a function call
                //then, the next it's either a close paren or an expression
                next = ReadNext();
                if (next is TokenCloseParen) //a close paren creates a function call without arguments
                {
                    return new Call(functionIdentifier.Identifier);
                }
                else
                {
                    var exprList = ParseExpressionList(true);
                    return new Call(functionIdentifier.Identifier, exprList);
                }

            }
            else return null; //it's not a function call. What is the parser expecting? Return null and let the caller decide
        }


        public Attribution ParseAttribution(TokenIdentifier functionIdentifier)
        {
            if (Peek() != functionIdentifier)
                throw new ParseFatalException($"The current node ({Peek().ToString()}) is not the same as the required ({functionIdentifier.ToString()}). The parser is about to possibly parse an attribution to a variable, but it cannot determine the variable name.", this);

            var next = ReadNext();

            if (next is TokenAttribution) //good chances it's an attribution
            {
                next = ReadNext();
                var expr = ParseExpression(false);

                var tok = Peek();
                if (tok is TokenNewLine || tok is TokenEOF) return new Attribution(functionIdentifier, expr);
                else throw new ParseException("Expected a newline or end of file, got " + tok.ToString(), this);
            }
            else return null; //it's not an attribution. What is the parser expecting? Return null and let the caller decide

        }

        public enum Associativity
        {
            Left, Right
        }


        public List<object> PrepareTokensForSingleExprParsing(bool isInFunction, out bool endedInParenthesis, out bool endedInComma)
        {
            endedInComma = false;
            List<object> input = new List<object>();

            //stops at TokenComma or TokenNewLine

            int pendingCloseParenthesis = isInFunction ? 1 : 0;

            object previous = null;

            while (true)
            {
                object current = Peek();
                //indicates if the collection of tokens should stop
                bool shouldBreak = false;

                //if its in a function call, we'll get tokens as long as we have a odd number of pending close parenthesis to be found. Or newline/eof/comma, ofc
                //if ends in comma, we have to account for a close paren still pending 
                //if its not in a function,then we get until newline/eof


                //If the current token (read by Peek()) is a newline/eof, the previous one coul possibily be a closeparen.
                //so we stop here.
                //if Peek() returns a comma, then again the previous one could possibly be a closeparen, therefore stop
                if (current is TokenNewLine || current is TokenEOF) { break; }
                else if (current is TokenComma)
                {
                    if (isInFunction) //this is where we account for a parenthesis that will eventually be picked up in the other expression (assuming code is valid)
                    {
                        pendingCloseParenthesis--;
                        shouldBreak = true;
                    }
                }
                else if (current is TokenIdentifier tokenIdentifier)
                {
                    var call = ParseFunctionCall(tokenIdentifier);

                    //failing to parse a function call (that is, it's not actually a function call) causes the "cursor" to advance one more 
                    //token than what we actually need. It effectively jumps to the next 
                    //iteration that we are expecting in this while loop. Therefore, we reset/confirm the enumerator depending on that, creating a checkpoint to go
                    //"back in time"

                    if (call == null)
                    {
                        input.Add(current);
                        previous = current;
                        current = Peek();
                        continue;
                    }
                    else
                    {
                        current = call;
                    }
                }
                else if (current is TokenOpenParen)
                {
                    pendingCloseParenthesis++;
                }
                else if (current is TokenCloseParen)
                {
                    pendingCloseParenthesis--;

                    if (isInFunction && pendingCloseParenthesis == 0) //end of the expression when last param closes, only valid in a function context
                    {
                        shouldBreak = true; //then schedule a break;
                    }
                }

                input.Add(current);
                previous = current;

                if (!shouldBreak)
                    current = ReadNext();

                //avoid nonsensical syntax
                if (previous is TokenOpenParen && current is TokenCloseParen)
                {
                    throw new ParseException("Empty parenthesis not allowed in an expression", this);
                }

                if (shouldBreak) break;
            }

            if (input.Count == 0)
                throw new ParseException("Expression expected", this);

            if (isInFunction)
            {
                //TODO: needs some refactoring

                //if its inside a function, it is expected to end on a comma OR closing parenthesis
                //so those are the allowed situations:
                bool finishedInParenthesis = input.Last() is TokenCloseParen && pendingCloseParenthesis == 0;
                bool finishedInComma = input.Last() is TokenComma && pendingCloseParenthesis == 0; //if finishes in comma, all parenthesis should be alright

                if (finishedInParenthesis)
                {

                    endedInParenthesis = true;
                    endedInComma = false;

                    input.Remove(input.Last());

                }
                else
                {
                    if (!finishedInComma) //if finished in comma, will fall on else and continue execution
                    {
                        //otherwise, fails. TODO: I dont know exactly what to write here. Refactor code so variables make more sense.
                        if (!finishedInParenthesis && previous is TokenCloseParen)
                        {
                            throw new ParseException("Could not find the end of an expression due to a missing closing parenthesis inside a function call. Add the missing parenthesis at the end of this function call.", this);
                        }
                        else
                        {
                            //TODO: i'm not sure
                            throw new ParseException("Could not recognize the expression, probably due to a misplaced comma inside a function call.", this);
                        }
                    }
                    else
                    {
                        endedInComma = true;
                        endedInParenthesis = false;

                        input.Remove(input.Last());
                    }
                }
            }
            else
            {
                endedInParenthesis = false;
                endedInComma = false;
            }

            //all parenthesis should be closed when it gets here
            if (pendingCloseParenthesis > 0)
            {
                throw new ParseException("Could not find the end of an expression due to a missing closing parenthesis", this);
            }
            else if (pendingCloseParenthesis < 0)
            {
                throw new ParseException("Could not understand the expression due to an extra closing parenthesis", this);
            }

            if (input.Count == 0)
                throw new ParseException("Expression expected", this);

            return input;
        }

        public ExpressionList ParseExpressionList(bool isInFunction)
        {
            List<Expression> expressions = new List<Expression>();
            bool endedInParenthesis = false, endedInComma = false;
            while (true)
            {
                var tokens = PrepareTokensForSingleExprParsing(isInFunction: isInFunction, endedInParenthesis: out endedInParenthesis, endedInComma: out endedInComma);

                var expr = ParseExprFromInput(tokens);

                expressions.Add(expr);

                if (!endedInComma) break;
                else ReadNext();
            }
            //sanity check
            if (!(expressions.Count > 0 && endedInParenthesis && !endedInComma))
            {
                throw new ParseException("The list of expressions inside a function call should end with a closing parenthesis", this);
            }

            return new ExpressionList(expressions);
        }

        public Expression ParseExpression(bool isInFunction)
        {
            var tokens = PrepareTokensForSingleExprParsing(isInFunction: isInFunction, endedInParenthesis: out bool endedInParenthesis, endedInComma: out bool endedInComma);
            if (endedInComma)
            {
                throw new ParseException("Unexpected comma", this);
            }
            else
            {
                return ParseExprFromInput(tokens);
            }
        }

        //  10 && 10
        //seems like all of these should be parser nodes and not a mixture of tokens and nodes.
        //but whatever
        public static Dictionary<Type, (int precedence, Associativity associativity, bool isOp)> PrecedenceAndAssociativity
            = new Dictionary<Type, (int precedence, Associativity associativity, bool isOp)>()
        {
            { typeof(TokenOpenParen), (-999, Associativity.Left, true) },
            { typeof(TokenCloseParen), (-999, Associativity.Left, true) },



            { typeof(Call), (100, Associativity.Left, false) },

            { typeof(TokenIdentifier), (100, Associativity.Left, false) },
            { typeof(TokenLiteralNumber), (100, Associativity.Left, false) },
            { typeof(TokenLiteralString), (100, Associativity.Left, false) },
            { typeof(TokenBooleanLiteralTrue), (100, Associativity.Left, false) },
            { typeof(TokenBooleanLiteralFalse), (100, Associativity.Left, false) },
            { typeof(TokenNot), (100, Associativity.Left, true) },

            { typeof(TokenMultiply), (80, Associativity.Left, true) },
            { typeof(TokenDivide), (80, Associativity.Left, true) },

            { typeof(TokenPlus), (70, Associativity.Left, true) },
            { typeof(TokenMinus), (70, Associativity.Left, true) },



            { typeof(TokenEquals), (50, Associativity.Left, true) },
            { typeof(TokenNotEquals), (50, Associativity.Left, true) },
            { typeof(TokenGreaterOrEqualsThan), (50, Associativity.Left, true) },
            { typeof(TokenGreaterThan), (50, Associativity.Left, true) },
            { typeof(TokenLowerOrEqualsThan), (50, Associativity.Left, true) },
            { typeof(TokenLowerThan), (50, Associativity.Left, true) },

            { typeof(TokenAnd), (50, Associativity.Left, true) },
            { typeof(TokenOr), (50, Associativity.Left, true) },
        };


        //infix-prefix conversion
        //closing parens remove all ops from stack
        //understands function calls 
        //NOT operators are simply added to the opstack, exprcheck, typecheck and codegen/interpreter have to be smart and 
        //unstack from the expr stack only 1 item and apply its operation to them
        //they have high precedence so they hopefully will be popped from the opstack asap
        //like so:

        //false && !true => false true ! &&
        //false true ! &&
        //false [true !] &&
        //[false false &&]
        //true

        //!(false & true) => false true && !
        //false true && !
        //[false true &&] !
        //false !
        //true


        // !((10 + 20) == (30 + 40))
        // [10 20 +] 30 40 + == !  
        // [30] [30 40 +] == !
        // [[30] [70] ==] !
        // [false !]
        // true

        public Expression ParseExprFromInput(List<object> input) //http://scriptasylum.com/tutorials/infix_postfix/algorithms/infix-postfix/index.htm
        {
            Stack<object> opstack = new Stack<object>();
            List<object> expr = new List<object>();

            //Scan the Infix string from left to right.
            foreach (var obj in input)
            {
                var type = obj.GetType();
                var precAssoc = PrecedenceAndAssociativity[type];

                if (!precAssoc.isOp) //If the scannned character is an operand, add it to the Postfix string.
                {
                    expr.Add(obj);
                }
                else if (obj is TokenOpenParen) //If the scanned character is an operator
                {
                    opstack.Push(obj);
                }
                else if (obj is TokenCloseParen)
                {
                    object topToken = opstack.Pop();
                    while (!(topToken is TokenOpenParen) && opstack.Count > 0)
                    {
                        expr.Add(topToken);
                        topToken = opstack.Pop();
                    }
                    if (!(topToken is TokenOpenParen))
                    {
                        throw new ParseException("Unmatched )", this);
                    }
                }
                else
                {
                    if (opstack.Count == 0)
                    {
                        opstack.Push(obj);
                    }
                    else
                    {
                        while (opstack.Count > 0 &&
                            PrecedenceAndAssociativity[opstack.Peek().GetType()].precedence >= precAssoc.precedence)
                        {
                            expr.Add(opstack.Pop());
                        }

                        opstack.Push(obj);
                    }
                }
            }

            while (opstack.Count > 0)
            {
                expr.Add(opstack.Pop());
            }

            CheckExpressionIsComplete(expr);

            return new Expression(expr);
        }

        //checks if the expression makes sense at all, that is, there's no extra operators and operands
        //it must avoid things like(1 + 1 1 1 1 1 1) that become 1 1 1 1 1 1 1 1 + which makes no sense at all
        public bool CheckExpressionIsComplete(List<object> expr)
        {
            return true;
        }


        public BaseToken Read()
        {
            var ret = EnumeratorTokens.Current;
            EnumeratorTokens.MoveNext();
            return ret;
        }

        public BaseToken ReadNext()
        {
            if (!EnumeratorTokens.MoveNext()) return null;
            else return EnumeratorTokens.Current;
        }

        public BaseToken Peek() => EnumeratorTokens.Current;

    }
}