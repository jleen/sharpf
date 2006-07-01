/*
 * parse.cs:
 *
 * Parse a token stream into Scheme objects.  I'm proud of how little code
 * there is here.  Parsing Lisp turns out to be trivial.  Parse and
 * ParseList are mutually recursive:
 *
 *   - ParseList invokes Parse to parse each individual object in the
 *     stream, and bundles them up into a Scheme list.
 *
 *   - Parse invokes ParseList whenever it encounters a left paren, and
 *     returns the head of the resulting list.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                case TokenType.Integer: {
                    // TODO: Be less cheesy about integers.  Either *use*
                    // the Integer class, or get rid of it.
                    try
                    {
                        return new Rational(BigNum.Parse(token.text), 1);
                    }
                    catch (OverflowException)
                    {
                        throw new BogusArithmeticException(token.text);
                    }
                }
                case TokenType.Rational: {
                    string[] nums = token.text.Split(new char[] { '/' }, 2);
                    BigNum num;
                    BigNum denom;
                    try
                    {
                        num = BigNum.Parse(nums[0]);
                        denom = BigNum.Parse(nums[1]);
                    }
                    catch (OverflowException)
                    {
                        throw new BogusArithmeticException(token.text);
                    }
                    return new Rational(num, denom);
                }
                case TokenType.Boolean: {
                    if (token.text.ToLower() == "#t")
                        return new Boolean(true);
                    else
                        return new Boolean(false);
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

            Debug.Assert(
                false,
                "I don't know how to parse " + token.type.ToString());
            return new Unspecified();
        }
    }
}
