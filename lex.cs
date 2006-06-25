/*
 * lex.cs:
 *
 * Quick and dirty lexer using regular expressions.  There's probably a
 * better way, but I wasn't much interested in writing a lexer.
 */

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SaturnValley.SharpF
{
    enum TokenType
    {
        Whitespace,
        Identifier,
        Boolean,
        Integer,
        Rational,
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
            // The vocabulary definition.  Every regex must start with a
            // caret!

            new TokenData(TokenType.Whitespace,
                          new Regex(@"^([ \t]+|;.*)")),
            new TokenData(TokenType.Identifier,
                          new Regex(@"^([" + symbolChars + @"]" +
                                    @"[" + symbolChars + subseqChars + @"]*" +
                                    @"|\+|-|\.\.\.)" + wordBoundary)),
            new TokenData(TokenType.Integer,
                          new Regex("^-?[0-9]+" + wordBoundary)),
            new TokenData(TokenType.Rational,
                          new Regex("^-?[0-9]+/[0-9]+" + wordBoundary)),
            new TokenData(TokenType.Open,
                          new Regex(@"^\(")),
            new TokenData(TokenType.Close,
                          new Regex(@"^\)")),
            new TokenData(TokenType.Quote,
                          new Regex(@"^'")),
            new TokenData(TokenType.String,
                          new Regex(@"^""[^""]*""")),
            new TokenData(TokenType.Boolean,
                          new Regex(@"^#[TtFf]"))
        };

        // We use yield return to generate a stream of tokens that will be
        // consumed by the parser.  I like this because it really
        // illustrates the point of coroutines, as execution flops back and
        // forth between Lex and Parse without anyone having to explicitly
        // juggle their state.  This is "yield" not in the sense of crop
        // yield, but as in yielding the right of way.
        //
        // Note that I use goto where I could almost certainly use break.
        // Goto seems clearer; when there are this many nested loops, it's
        // unclear where break will take us, and it'll be fragile when we
        // refactor.

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

                    throw new TokenException(line.Substring(pos));
                okay:;
                }
            }
        }
    }
}
