/*
 * prims.cs:
 *
 * Implementation of primitive procedures with attribute-driven dispatch.
 * Y'know, even with attributes there's too much boilerplate in this file.
 * I should use a macro language.  It's really big of the CLR people to try
 * to wean us off C-style macros, but there's just too much they're good
 * for.  The CLR folks have anticipated a lot, but it's precisely what you
 * *don't* anticipate that macros are good for.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SaturnValley.SharpF
{
    // The attribute which will direct us to bind each primitive procedure
    // to its symbol name.  A primitive with a "magic argument" will
    // be passed some implicit context (like the environment) as an
    // explicit argument.

    public class PrimitiveAttribute : Attribute
    {
        public string name;
        public Primitives.MagicArgs magicArgs;

        public PrimitiveAttribute(string n)
        {
            name = n;
            magicArgs = Primitives.MagicArgs.None;
        }

        public PrimitiveAttribute(string n, Primitives.MagicArgs ma)
        {
            name = n;
            magicArgs = ma;
        }
    }

    public static class Primitives
    {
        [Flags]
        public enum MagicArgs
        {
            None = 0,
            Environment = 1
        }

        // Type-checking utilities.

        private static void CheckType(string who, int i, Datum arg, Type t)
        {
            if (arg != null)
            {
                if (t == typeof(Integer))
                {
                    if (((Rational)arg).Denom.CompareTo(1) != 0)
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

        private static void RequireMultiArgs(
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

        private static void RequireArgs(
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

        // Types

        [Primitive("pair?")]
        public static Datum PairP(List<Datum> args)
        {
            return new Boolean(args[0] is Pair);
        }

        // Equality

        [Primitive("eqv?")]
        public static Datum EqvP(List<Datum> args)
        {
            Datum a = args[0];
            Datum b = args[1];

            if (a is Rational)
            {
                if (!(b is Rational))
                    return new Boolean(false);

                Rational i = (Rational)args[0];
                Rational j = (Rational)args[1];
                return new Boolean(i.CompareTo(j) == 0);
            }
            else if (a is String)
            {
                if (!(b is String))
                    return new Boolean(false);

                String s = (String)a;
                String t = (String)b;
                return new Boolean(s.val == t.val);
            }
            else if (a is Symbol)
            {
                if (!(b is Symbol))
                    return new Boolean(false);

                Symbol x = (Symbol)a;
                Symbol y = (Symbol)b;
                return new Boolean(x.name == y.name);
            }
            else
            {
                return new Boolean(a == b);
            }
        }

        // Arithmetic

        [Primitive("+")]
        public static Datum Add(List<Datum> args)
        {
            RequireMultiArgs("+", args, 0, typeof(Number));

            Rational result = new Rational(0, 1);
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                result.Add(n);
            }
            return result;
        }

        [Primitive("-")]
        public static Datum Subtract(List<Datum> args)
        {
            RequireMultiArgs("-", args, 1, typeof(Number));

            Rational n = (Rational)args[0];
            Rational result;
            if (args.Count == 1)
            {
                result = new Rational(n);
                result.Negate();
            }
            else
            {
                result = new Rational((Rational) args[0]);
                for (int i = 1; i < args.Count; ++i)
                {
                    result.Subtract((Rational) args[i]);
                }
            }

            return result;
        }

        [Primitive("*")]
        public static Datum Multiply(List<Datum> args)
        {
            RequireMultiArgs("*", args, 0, typeof(Number));

            Rational result = new Rational(1, 1);
            foreach (Datum d in args)
            {
                Rational n = (Rational)d;
                result.Multiply(n);
            }
            return result;
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
            BigNum n1 = ((Rational)args[0]).Num;
            BigNum n2 = ((Rational)args[1]).Num;

            return new Rational(BigNum.LongDiv(n1, n2), 1);
        }

        [Primitive("remainder")]
        public static Datum Remainder(List<Datum> args)
        {
            RequireArgs("remainder", args, typeof(Integer), typeof(Integer));
            BigNum n1 = ((Rational)args[0]).Num;
            BigNum n2 = ((Rational)args[1]).Num;
            
            return new Rational(BigNum.Remainder(n1, n2), 1);
        }
            
        [Primitive("round")]
        public static Datum Round(List<Datum> args)
        {
            RequireArgs("round", args, typeof(Rational));
            Rational r = (Rational)args[0];
            return new Rational(r.Round(), 1);
        }

        [Primitive("floor")]
        public static Datum Floor(List<Datum> args)
        {
            RequireArgs("floor", args, typeof(Rational));
            Rational r = (Rational)args[0];

            return new Rational(r.Floor(), 1);
        }

        [Primitive("=")]
        public static Datum NumEqual(List<Datum> args)
        {
            RequireArgs("=", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.CompareTo(j) == 0);
        }

        [Primitive(">")]
        public static Datum GreaterThan(List<Datum> args)
        {
            RequireArgs(">", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.CompareTo(j) > 0);
        }

        [Primitive(">=")]
        public static Datum GreaterEqual(List<Datum> args)
        {
            RequireArgs(">=", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.CompareTo(j) >= 0);
        }

        [Primitive("<")]
        public static Datum LessThan(List<Datum> args)
        {
            RequireArgs("<", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.CompareTo(j) < 0);
        }

        [Primitive("<=")]
        public static Datum LessThanEqual(List<Datum> args)
        {
            RequireArgs("<", args, typeof(Number), typeof(Number));

            Rational i = (Rational)args[0];
            Rational j = (Rational)args[1];
            return new Boolean(i.CompareTo(j) <= 0);
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

        [Primitive("even?")]
        public static Datum EvenP(List<Datum> args)
        {
            RequireArgs("even?", args, typeof(Number));

            Rational r = (Rational) args[0];

            return new Boolean(r.Even());
        }

        [Primitive("length")]
        public static Datum Length(List<Datum> args)
        {
            Datum list = args[0];

            int len = 0;
            while (list != null)
            {
                ++len;
                list = list.Cdr;
            }

            return new Rational(len, 1);
        }

        // Strictly speaking, we have a problem here, which is that we're
        // calling the evaluator on the CLR stack.  I could fix this by
        // handling it inline, as CALL/CC; or by implementing it in Scheme.
        // But this'll do for now.

        [Primitive("map", MagicArgs.Environment)]
        public static Datum Map(List<Datum> args)
        {
            // TODO: This is going to require some interesting
            // type-checking.

            // Shift!
            Environment env = (Environment)args[0];
            args.RemoveAt(0);

            // Shift!
            Datum func = args[0];
            args.RemoveAt(0);

            List<Datum> results = new List<Datum>();
            while (args[0] != null)
            {
                Pair slice = new Pair(func, null);
                for (int i = 0; i < args.Count; i++)
                {
                    slice = new Pair(args[i].Car, slice);
                    args[i] = args[i].Cdr;
                }
                Datum res = Evaluator.Act(new Evaluator.Action(
                    Evaluator.Actor.Apply,
                    slice,
                    env,
                    null));
                results.Add(res);
            }
            return List(results);
        }

        [Primitive("current-environment", MagicArgs.Environment)]
        public static Datum CurrentEnvironment(List<Datum> args)
        {
            return args[0];
        }

        [Primitive("scheme-report-environment")]
        public static Datum SchemeReportEnvironment(List<Datum> args)
        {
            return Environment.CreateDefaultEnvironment();
        }

        [Primitive("eval")]
        public static Datum Eval(List<Datum> args)
        {
            Datum exp = args[0];
            Environment env = (Environment)args[1];

            return Evaluator.Act(new Evaluator.Action(
                Evaluator.Actor.Eval,
                exp,
                env,
                null));
        }

        [Primitive("for-each", MagicArgs.Environment)]
        public static Datum ForEach(List<Datum> args)
        {
            Map(args);
            return new Unspecified();
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
            // This "implementation" is just a stub so that the CALL/CC
            // symbol will exist.  The actual CALL/CC will be handled as a
            // special case inside the evaluator's Apply action.  It needs
            // to be that way so that CALL/CC will get called
            // tail-recursively.
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

        // Utilities

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

        // Initialization.  Scan this class for methods with the
        // PrimitiveAttribute, create delegates, and store them in Datum
        // objects.  Reflection is slow, but by creating delegates we pay
        // the cost up front and once only.  Invocation of a primitive is
        // just a method call on the delegate object, and thus fast.

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
                            primAttr.magicArgs,
                            env);
                    }
                }
            }
        }

        private static void BindPrimitive(string name,
                                          PrimitiveImplementation impl,
                                          MagicArgs magicArgs,
                                          Environment env)
        {
            bool magicEnv = (magicArgs & MagicArgs.Environment) != 0;
            env.Bind(new Symbol(name),
                     new Primitive(name, impl, magicEnv));
        }
    }
}
