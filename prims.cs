using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SaturnValley.SharpF
{
    public class PrimitiveAttribute : Attribute
    {
        public string name;

        public PrimitiveAttribute(string n)
        {
            name = n;
        }
    }

    static class Primitives
    {
        public static void CheckType(string who, int i, Datum arg, Type t)
        {
            if (arg != null)
            {
                if (t == typeof(Integer))
                {
                    if (((Rational)arg).Denom != 1)
                    {
                        throw new ArgumentTypeException(
                            who, i, arg.GetType(), typeof(Integer));
                    }

                    t = typeof(Rational);
                }

                if (!t.IsAssignableFrom(arg.GetType()))
                    throw new ArgumentTypeException(who, i, arg.GetType(), t);
            }
            if (arg == null &&
                !t.IsAssignableFrom(typeof(Datum)))
            {
                throw new ArgumentTypeException(who, i, null, t);
            }
        }

        public static void RequireMultiArgs(
            string who, List<Datum> what, int n, Type t)
        {
            if (what.Count < n)
                throw new MissingArgumentException(who, what.Count, n);

            int i = 1;
            foreach (Datum arg in what)
            {
                CheckType(who, i, arg, t);
                ++i;
            }
        }

        public static void RequireArgs(
            string who, List<Datum> what, params Type[] types)
        {
            if (what.Count != types.Length)
                throw new MissingArgumentException(
                    who, what.Count, types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                CheckType(who, i + 1, what[i], types[i]);
            }
        }

        // Arithmetic
        [Primitive("+")]
        public static Datum Add(List<Datum> args)
        {
            RequireMultiArgs("+", args, 0, typeof(Number));

            int Num = 0;
            int Denom = 1;
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                int oldDenom = Denom;
                Num *= n.Denom;
                Denom *= n.Denom;
                Num += oldDenom * n.Num;
            }
            return new Rational(Num, Denom);
        }

        [Primitive("-")]
        public static Datum Subtract(List<Datum> args)
        {
            RequireMultiArgs("-", args, 1, typeof(Number));

            Rational n = (Rational)args[0];
            if (args.Count == 1)
            {
                return new Rational(-n.Num, n.Denom);
            }
            else
            {
                args[0] = new Rational(-n.Num, n.Denom);
                Rational res = (Rational)Add(args);
                return new Rational(-res.Num, res.Denom);
            }
        }

        [Primitive("*")]
        public static Datum Multiply(List<Datum> args)
        {
            RequireMultiArgs("*", args, 0, typeof(Number));

            int Num = 1;
            int Denom = 1;
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                Num *= n.Num;
                Denom *= n.Denom;
            }
            return new Rational(Num, Denom);
        }

        [Primitive("/")]
        public static Datum Divide(List<Datum> args)
        {
            RequireMultiArgs("/", args, 1, typeof(Number));

            for (int i = 1; i < args.Count; i++)
                args[i] = ((Rational)args[i]).Reciprocal;
            return Multiply(args);
        }

        [Primitive("quotient")]
        public static Datum Quotient(List<Datum> args)
        {
            RequireArgs("quotient", args, typeof(Integer), typeof(Integer));

            int n1 = ((Rational)args[0]).Num;
            int n2 = ((Rational)args[1]).Num;
            return new Integer(n1 / n2);
        }

        [Primitive("remainder")]
        public static Datum Remainder(List<Datum> args)
        {
            RequireArgs("remainder", args, typeof(Integer), typeof(Integer));

            int n1 = ((Rational)args[0]).Num;
            int n2 = ((Rational)args[1]).Num;
            return new Integer(n1 - n2 * (n1 / n2));
        }
            
        [Primitive("=")]
        public static Datum NumEqual(List<Datum> args)
        {
            RequireArgs("=", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.Num == j.Num && i.Denom == j.Denom);
        }

        [Primitive("<")]
        public static Datum LessThan(List<Datum> args)
        {
            RequireArgs("<", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.Num * j.Denom < j.Num * i.Denom);
        }

        // Lists
        [Primitive("car")]
        public static Datum Car(List<Datum> args)
        {
            RequireArgs("car", args, typeof(Pair));

            Pair p = (Pair)args[0];
            return p.car;
        }

        [Primitive("cdr")]
        public static Datum Cdr(List<Datum> args)
        {
            RequireArgs("cdr", args, typeof(Pair));

            Pair p = (Pair)args[0];
            return p.cdr;
        }
        
        [Primitive("cons")]
        public static Datum Cons(List<Datum> args)
        {
            RequireArgs("car", args, typeof(Datum), typeof(Datum));

            return new Pair(args[0], args[1]);
        }

        [Primitive("list")]
        public static Datum List(List<Datum> args)
        {
            Pair list = null;
            for (int i = args.Count - 1; i>= 0; i--)
                list = new Pair(args[i], list);
            return list;
        }

        [Primitive("null?")]
        public static Datum NullP(List<Datum> args)
        {
            RequireArgs("null?", args, typeof(Datum));

            return new Boolean(args[0] == null);
        }

        // Strings
        [Primitive("string-append")]
        public static Datum StringAppend(List<Datum> args)
        {
            RequireMultiArgs("string-append", args, 0, typeof(String));

            StringBuilder sb = new StringBuilder();

            foreach (Datum d in args)
            {
                String s = (String)d;
                sb.Append(s.val);
            }

            return new String(sb.ToString());
        }

        [Primitive("number->string")]
        public static Datum NumberToString(List<Datum> args)
        {
            RequireArgs("number->string", args, typeof(Number));

            return new String(Shell.Format(args[0]));
        }

        // Display
        [Primitive("display")]
        public static Datum Display(List<Datum> args)
        {
            RequireArgs("display", args, typeof(Datum));

            String s = args[0] as String;
            if (s != null)
            {
                Console.Write(s.val);
            }
            else
            {
                Shell.Print(args[0]);
            }
            return new Unspecified();
        }

        [Primitive("newline")]
        public static Datum Newline(List<Datum> args)
        {
            RequireArgs("newline", args);

            Console.WriteLine();
            return new Unspecified();
        }
            
        // Wiiiii
        [Primitive("call-with-current-continuation")]
        public static Datum CallCC(List<Datum> args)
        {
            return new Unspecified();
        }

        // Meta
        [Primitive("quit")]
        public static Datum Quit(List<Datum> args)
        {
            RequireArgs("quit", args);

            Console.WriteLine("\nENTER PREFIX (PRESS \"RETURN\" TO ACCEPT)");
            System.Environment.Exit(0);
            return new Unspecified();
        }

        [Primitive("load")]
        public static Datum Load(List<Datum> args)
        {
            RequireArgs("load", args, typeof(String));

            String filename = (String)args[0];
            return LoadInternal(filename.val);
        }

        public static Datum LoadInternal(string filename)
        {
            Datum d = new Unspecified();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() != -1)
                    d = Evaluator.Act(
                        new Evaluator.Action(
                            Evaluator.Actor.Eval,
                            Shell.Read(sr),
                            Environment.Toplevel,
                            null));
            }
            return d;
        }

        public static void BindPrimitives(Environment env)
        {
            foreach (MethodInfo meth in typeof(Primitives).GetMethods())
            {
                foreach (Attribute attr in meth.GetCustomAttributes(false))
                {
                    PrimitiveAttribute primAttr = attr as PrimitiveAttribute;
                    if (primAttr != null)
                    {
                        BindPrimitive(
                            primAttr.name,
                            (PrimitiveImplementation)Delegate.CreateDelegate(
                                typeof(PrimitiveImplementation), meth),
                            env);
                    }
                }
            }
        }

        private static void BindPrimitive(string name,
                                          PrimitiveImplementation impl,
                                          Environment env)
        {
            env.Bind(new Symbol(name),
                     new Primitive(name, impl));
        }
    }
}
