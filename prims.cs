using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
        public static void RequireMultiArgs(
            string who, List<Datum> what, int n, Type t)
        {
            if (what.Count < n)
                throw new MissingArgumentException(who, what.Count, n);

            int i = 1;
            foreach (Datum arg in what)
            {
                if (arg != null &&
                    !t.IsAssignableFrom(arg.GetType()))
                {
                    throw new ArgumentTypeException(who, i, arg.GetType(), t);
                }
                ++i;
            }
        }

        // Arithmetic
        [Primitive("+")]
        public static Datum Add(List<Datum> args)
        {
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
            Rational n = (Rational)args[0];
            n.Reciprocal();
            Rational res = (Rational)Add(args);
            res.Reciprocal();
            return res;
        }

        [Primitive("=")]
        public static Datum NumEqual(List<Datum> args)
        {
            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.Num == j.Num && i.Denom == j.Denom);
        }

        [Primitive("<")]
        public static Datum LessThan(List<Datum> args)
        {
            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.Num * j.Denom < j.Num * i.Denom);
        }

        // Lists
        [Primitive("car")]
        public static Datum Car(List<Datum> args)
        {
            Pair p = (Pair)args[0];
            return p.car;
        }

        [Primitive("cdr")]
        public static Datum Cdr(List<Datum> args)
        {
            Pair p = (Pair)args[0];
            return p.cdr;
        }
        
        [Primitive("cons")]
        public static Datum Cons(List<Datum> args)
        {
            return new Pair(args[0], args[1]);
        }

        [Primitive("list")]
        public static Datum List(List<Datum> args)
        {
            Pair list = null;
            for (int i = args.Count - 1; i>= 0; i++)
                list = new Pair(args[i], list);
            return list;
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
            Console.WriteLine("\nENTER PREFIX (PRESS \"RETURN\" TO ACCEPT)");
            System.Environment.Exit(0);
            return new Unspecified();
        }

        [Primitive("load")]
        public static Datum Load(List<Datum> args)
        {
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
                        new Evaluator.Activation(
                            Evaluator.Actor.Eval,
                            Shell.Read(sr),
                            Environment.Toplevel));
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
