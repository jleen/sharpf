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
        // Arithmetic
        [Primitive("+")]
        public static Datum Add(List<Datum> args)
        {
            int num = 0;
            int denom = 1;
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                int oldDenom = denom;
                num *= n.denom;
                denom *= n.denom;
                num += oldDenom * n.num;
            }
            return new Rational(num, denom);
        }

        [Primitive("-")]
        public static Datum Subtract(List<Datum> args)
        {
            Rational n = (Rational)args[0];
            if (args.Count == 1)
            {
                return new Rational(-n.num, n.denom);
            }
            else
            {
                n.num = -n.num;
                Rational res = (Rational)Add(args);
                res.num = -res.num;
                return res;
            }
        }

        [Primitive("*")]
        public static Datum Add(List<Datum> args)
        {
            int num = 1;
            int denom = 1;
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                num *= n.num;
                denom *= n.denom;
            }
            return new Rational(num, denom);
        }

        [Primitive("/")]
        public static Datum Subtract(List<Datum> args)
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
            return new Boolean(i.num == j.num && i.denom == j.denom);
        }

        [Primitive("<")]
        public static Datum LessThan(List<Datum> args)
        {
            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.num * j.denom < j.num * i.denom);
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

        // Meta
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
                    d = Evaluator.Trampoline(
                        new Evaluator.TrampCall(
                            Evaluator.TrampTarget.Eval,
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
