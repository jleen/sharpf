using System.Collections.Generic;
using System.IO;

namespace SaturnValley.SharpF
{
    static class Primitives
    {
        // Arithmetic
        public static Datum Add(List<Datum> args)
        {
            int accum = 0;
            foreach (Datum d in args)
            {
                Number n = (Number)d;
                accum += n.val;
            }
            return new Number(accum);
        }
        public static Datum Subtract(List<Datum> args)
        {
            Number i = (Number)args[0];
            Number j = (Number)args[1];
            return new Number(i.val - j.val);
        }
        public static Datum NumEqual(List<Datum> args)
        {
            Number i = (Number)args[0];
            Number j = (Number)args[1];
            return new Boolean(i.val == j.val);
        }
        public static Datum LessThan(List<Datum> args)
        {
            Number i = (Number)args[0];
            Number j = (Number)args[1];
            return new Boolean(i.val < j.val);
        }

        // Lists
        public static Datum Car(List<Datum> args)
        {
            Pair p = (Pair)args[0];
            return p.car;
        }
        public static Datum Cdr(List<Datum> args)
        {
            Pair p = (Pair)args[0];
            return p.cdr;
        }
        public static Datum Cons(List<Datum> args)
        {
            return new Pair(args[0], args[1]);
        }
        public static Datum List(List<Datum> args)
        {
            Pair list = null;
            for (int i = args.Count - 1; i>= 0; i++)
                list = new Pair(args[i], list);
            return list;
        }

        // Meta
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

        public static void BindPrimitives(Environment e)
        {
            BindPrimitive("+", Add, e);
            BindPrimitive("-", Subtract, e);
            BindPrimitive("=", NumEqual, e);
            BindPrimitive("<", LessThan, e);
            BindPrimitive("car", Car, e);
            BindPrimitive("cdr", Cdr, e);
            BindPrimitive("cons", Cons, e);
            BindPrimitive("list", List, e);
            BindPrimitive("load", Load, e);
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
