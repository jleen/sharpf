using System;

namespace SaturnValley.SharpF
{
    public static class Shell
    {
        public static void Main(string[] args)
        {
            System.Console.WriteLine("#f");
                
            Dump(Parser.Parse(
                Lexer.Lex(Console.OpenStandardInput()).GetEnumerator()));
        }

        public static void Dump(Datum a)
        {
            Dump(a, String.Empty);
        }

        public static void Dump(Datum a, string prefix)
        {
            if (a == null)
            {
                Console.WriteLine(prefix + "NIL");
            }
            else if (a is Symbol)
            {
                Console.WriteLine(prefix + "SYMBOL " +
                                  (a as Symbol).name);
            }
            else if (a is Number)
            {
                Console.WriteLine(prefix + "NUMBER " +
                                 (a as Number).val.ToString());
            }
            else if (a is Pair)
            {
                Console.WriteLine(prefix + "PAIR:");
                Pair p = a as Pair;
                string newpref = prefix + "    ";
                Dump(p.car, newpref);
                Dump(p.cdr, newpref);
            }
            else
            {
                Console.WriteLine(prefix + "DUMP ERROR: " + a.ToString());
            }
        }
    }
}
