using System;
using VeryBasic.Token;
using System.Linq;
using VeryBasic.Parser;

class Program
{
    static void Main(string[] args)
    {

        //adicionar EndOfLine

        try
        {
            var source = System.IO.File.ReadAllText("language demo.basic");
            var tokens = new Tokenizer(source).Tokenize().ToList();

            foreach (var token in tokens)
            {
                Console.Write(token.ToString() + (token is TokenNewLine ? "" : " "));
            }

            var ast = new Parser(tokens).Parse().ToList();

            
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(ex.ToString());
        }

    }
}
