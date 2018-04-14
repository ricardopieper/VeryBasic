using System;
namespace VeryBasic.Parser
{
    public class ParseException : Exception
    {
         public ParseException(string message, Parser parser) 
            : base(message + ", at "+parser.Peek()) { }
    }

    public class ParseFatalException : Exception
    {
         public ParseFatalException(string message, Parser parser) 
            : base("PARSE FATAL ERROR: "+message) { }
    }

}