using System;
using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public static class Shell
    {
        public static void Main(string[] args)
        {
            Environment.Toplevel = Environment.CreateDefaultEnvironment();
            Primitives.LoadInternal("library.scm");
            System.Console.WriteLine("#f");
                
            while (true)
            {
                Print(
                    Evaluator.Trampoline(
                        new Evaluator.TrampCall(
                                Evaluator.TrampTarget.Eval,
                                Read(new System.IO.StreamReader(
                                    Console.OpenStandardInput())),
                                Environment.Toplevel)));
                System.Console.WriteLine();
            }
        }

        public static Datum Read(System.IO.StreamReader sr)
        {
            IEnumerator<Token> tokens =
                Lexer.Lex(sr).GetEnumerator();
            tokens.MoveNext();
            return Parser.Parse(tokens);
        }

        public static void Print(Datum a)
        {
            if (a == null)
            {
                Console.Write("()");
            }
            else if (a is Symbol)
            {
                Console.Write((a as Symbol).name);
            }
            else if (a is Number)
            {
                Console.Write((a as Number).val.ToString());
            }
            else if (a is Boolean)
            {
                if ((a as Boolean).val == true)
                    Console.Write("#t");
                else
                    Console.Write("#f");
            }
            else if (a is Pair)
            {
                Console.Write("(");
                while (a is Pair)
                {
                    Pair p = a as Pair;
                    Print(p.car);
                    if (p.cdr != null)
                        Console.Write(" ");
                    a = p.cdr;
                }
                if (a != null)
                {
                    Console.Write(". ");
                    Print(a);
                }
                Console.Write(")");
            }
            else if (a is Primitive)
            {
                Console.Write("#<primitive " + (a as Primitive).name + ">");
            }
            else if (a is Closure)
            {
                Closure c = a as Closure;
                Console.Write("#<closure ");
                Print(c.formals);
                Console.Write(" ");
                Print(c.body);
                Console.Write(">");
            }
            else if (a is Unspecified)
            {
                Console.Write("#<unspecified>");
            }
            else if (a is SharpF.String)
            {
                Console.Write("\"" + (a as SharpF.String).val + "\"");
            }
            else
            {
                Console.Write("#<unprintable: " +
                                  a.ToString() + ">");
            }
        }
    }
}
