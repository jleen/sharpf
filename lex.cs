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
        const string symbolChars = @"!$%&*/:<=>?^_~A-Za-z";
        const string subseqChars = @"+-.@0-9";
        const string wordBoundary = @"(?=([ \t()';]|$))";

        public static readonly TokenData[] Tokens =
        {
            new TokenData(TokenType.Whitespace,
                          new Regex(@"^([ \t]+|;.*)")),
            new TokenData(TokenType.Identifier,
                          new Regex(@"^([" + symbolChars + @"]" +
                                    @"[" + symbolChars + subseqChars + @"]*" +
                                    @"|\+|-|\.\.\.)" + wordBoundary)),
            new TokenData(TokenType.Number,
                          new Regex("^[0-9]+" + wordBoundary)),
            new TokenData(TokenType.Open,
                          new Regex(@"^\(")),
            new TokenData(TokenType.Close,
                          new Regex(@"^\)")),
            new TokenData(TokenType.Quote,
                          new Regex(@"'")),
            new TokenData(TokenType.String,
                          new Regex(@"""[^""]*"""))
        };

        public static IEnumerable<Token> Lex(StreamReader sr)
        {
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
                            if (td.type != TokenType.Whitespace)
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
