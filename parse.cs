using System.Collections.Generic;
using System.IO;

namespace SaturnValley.SharpF
{
    internal static class Parser
    {
        public static Datum ParseList(IEnumerator<Token> tokens)
        {
            if (!tokens.MoveNext())
                return null;

            Token token = tokens.Current;
            if (token.type == TokenType.Close)
                return null;

            Datum car = Parse(tokens);
            Datum cdr = ParseList(tokens);
            return new Pair(car, cdr);
        }

        public static Datum Parse(IEnumerator<Token> tokens)
        {
            Token token = tokens.Current;
            switch (token.type)
            {
                case TokenType.Identifier: {
                    return new Symbol(token.text.ToLowerInvariant());
                }
                case TokenType.Number: {
                    return new Number(System.Int32.Parse(token.text));
                }
                case TokenType.Open: {
                    return ParseList(tokens);
                }
                case TokenType.Quote: {
                    tokens.MoveNext();
                    return new Pair(new Symbol("quote"),
                                    new Pair(Parse(tokens),
                                             null));
                }
                case TokenType.String: {
                    return new String(token.text.Substring(
                        1, token.text.Length - 2));
                }
            };

            return new Symbol("unknown:" + token.type.ToString());
        }
    }
}
