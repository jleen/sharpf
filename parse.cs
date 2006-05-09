using System.Collections.Generic;
using System.IO;

namespace SaturnValley.SharpF
{
    internal static class Parser
    {
        public static Datum ParseList(IEnumerator<Token> tokens)
        {
            Datum car = Parse(tokens);
            if (car == null)
                return null;
            Datum cdr = ParseList(tokens);
            return new Pair(car, cdr);
        }

        public static Datum Parse(IEnumerator<Token> tokens)
        {
            if (!tokens.MoveNext())
                return null;

            Token token = tokens.Current;
            switch (token.type)
            {
                case TokenType.Identifier: {
                    return new Symbol(token.text);
                }
                case TokenType.Number: {
                    return new Number(System.Int32.Parse(token.text));
                }
                case TokenType.Open: {
                    return ParseList(tokens);
                }
                case TokenType.Close: {
                    return null;
                }
            };

            return new Symbol("unknown:" + token.type.ToString());
        }
    }
}
