using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SaturnValley.SharpF
{
    enum TokenType
    {
        Error,
        Whitespace,
        Identifier,
        Boolean,
        Number,
        Character,
        String,
        Open,
        Close,
        SharpOpen,
        Quote,
        Quasiquote,
        Unquote,
        InterpUnquote,
        Dot
    }

    class Token
    {
        public TokenType type;
        public string text;

        public Token(TokenType t, string tx)
        {
            type = t; text = tx;
        }
    }

    class TokenData
    {
        public TokenType type;
        public Regex regex;

        public TokenData(TokenType t, Regex r)
        {
            type = t; regex = r;
        }
    }

    internal class Lexer
    {
        public static readonly TokenData[] Tokens =
        {
            new TokenData(TokenType.Whitespace,
                          new Regex(@"^([ \t]+|;.*)")),
            new TokenData(TokenType.Identifier,
                          new Regex("^[a-z]+")),
            new TokenData(TokenType.Number,
                          new Regex("^[0-9]+")),
            new TokenData(TokenType.Open,
                          new Regex(@"^\(")),
            new TokenData(TokenType.Close,
                          new Regex(@"^\)"))
        };

        public static IEnumerable<Token> Lex(Stream s)
        {
            StreamReader sr = new StreamReader(s);

            string line;
            while (null != (line = sr.ReadLine()))
            {
                int pos = 0;
                while (pos < line.Length)
                {
                    foreach (TokenData td in Tokens)
                    {
                        Match m = td.regex.Match(line.Substring(pos));
                        if (m.Success)
                        {
                            yield return new Token(td.type, m.Value);
                            pos += m.Length;
                            goto okay;
                        }
                    }

                    yield return new Token(TokenType.Error,
                                           line.Substring(pos));
                    yield break;
                okay:;
                }
            }
        }
    }
}
