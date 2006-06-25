/*
 * sharpf.cs:
 *
 * Entry point.  The REPL and some I/O and debugging code.
 *
 * The Trace functions really deserve to be in their own file, and Print
 * probably belongs in prims.cs.  Maybe some day.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
                Console.Write("\n]");
                try
                {
                    Print(
                        Evaluator.Act(
                            new Evaluator.Action(
                                Evaluator.Actor.Eval,
                                Read(new System.IO.StreamReader(
                                    Console.OpenStandardInput())),
                                Environment.Toplevel,
                                null)));
                }
                catch (SharpFException e)
                {
                    Console.Write("Oops!  " + e.Message);
                }

                Console.WriteLine();
            }
        }

        public static Datum Read(System.IO.StreamReader sr)
        {
            IEnumerator<Token> tokens =
                Lexer.Lex(sr).GetEnumerator();
            tokens.MoveNext();
            return Parser.Parse(tokens);
        }

        [Conditional("TRACE")]
        public static void TraceHeader()
        {
            Console.WriteLine(new System.String('=', 78));
        }

        [Conditional("TRACE")]
        public static void Trace(params object[] objs)
        {
            TraceHeader();
            TraceLine(objs);
            Console.WriteLine();
        }

        [Conditional("TRACE")]
        public static void TraceLine(params object[] objs)
        {
            foreach (Object o in objs)
            {
                if (o is string)
                {
                    Console.Write((string)o);
                }
                else if (o is Datum)
                {
                    Print((Datum)o);
                    string hash;
                    if (o == null)
                        hash = "null";
                    else
                        hash = o.GetHashCode().ToString();

                    Console.Write("{" + hash + "}");
                }
                else
                {
                    if (o == null)
                        Console.Write("{null}");
                    else
                        Console.Write(o.ToString());
                }
            }
        }

        [Conditional("TRACE")]
        public static void TraceAction(string what, Evaluator.Action call)
        {
            Shell.TraceHeader();
            while (call != null)
            {
                Shell.TraceLine(
                    what, " ", call.target,
                    "\nwith arg ", call.arg,
                    "\nresult ",
                    call.HasResult ?
                    (object)call.Result : (object)"{none}",
                    "\nand environment ", call.env.GetHashCode());
                call = call.next;
                what = "\nNext action will be";
            }
            Shell.TraceLine(what, " to return\n");
        }

        public static void Print(Datum a)
        {
            Console.Write(Format(a));
        }

        // I thought of doing Format as a virtual method of Datum, but I
        // prefer to keep all the formatting bundled up here.

        public static string Format(Datum a)
        {
            if (a == null)
            {
                return "()";
            }
            else if (a is Symbol)
            {
                return (a as Symbol).name;
            }
            else if (a is Rational)
            {
                Rational r = (Rational)a;
                StringBuilder fmt = new StringBuilder(r.Num.ToString());
                if (r.Denom != 1)
                {
                    fmt.Append("/");
                    fmt.Append(r.Denom.ToString());
                }
                return fmt.ToString();
            }
            else if (a is Boolean)
            {
                if ((a as Boolean).val == true)
                    return "#t";
                else
                    return "#f";
            }
            else if (a is Pair)
            {
                StringBuilder fmt = new StringBuilder("(");
                while (a is Pair)
                {
                    Pair p = a as Pair;
                    fmt.Append(Format(p.car));
                    if (p.cdr != null)
                        fmt.Append(" ");
                    a = p.cdr;
                }
                if (a != null)
                {
                    fmt.Append(". ");
                    fmt.Append(Format(a));
                }
                fmt.Append(")");
                return fmt.ToString();
            }
            else if (a is Primitive)
            {
                return "#<primitive " + (a as Primitive).name + ">";
            }
            else if (a is Closure)
            {
                Closure c = a as Closure;
                return "#<closure " + Format(c.formals) +
                    " " + Format(c.body) + ">";
            }
            else if (a is Continuation)
            {
                return "#<continuation " + a.GetHashCode() + ">";
            }
            else if (a is Unspecified)
            {
                return "#<unspecified>";
            }
            else if (a is SharpF.String)
            {
                return "\"" + (a as SharpF.String).val + "\"";
            }
            else
            {
                return "#<unprintable: " + a.ToString() + ">";
            }
        }
    }
}
