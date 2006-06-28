/*
 * data.cs:
 *
 * Data types.  A Datum is a Scheme object.  Datum is abstract but contains
 * a bunch of utilities.  It makes the Evaluator source *much* more
 * readable to be able to just say datum.Car without typing out a cast.  So
 * we do all the casting in the utility methods and, sure, throw if we have
 * to.  Casting "this" may be unorthodox, but it leads to clearer code.
 * Abstractly, it's physically possible to apply CAR and CDR operations to
 * any datum; it simply throws an error in the general case.  Which is
 * exactly right.
 *
 * Anyway, the actual data types are all subclasses.  We have three
 * different subclasses for procedures: Primitive, Closure, and
 * Continuation.  Maybe some day they should all be children of an abstract
 * Procedure, but right now there doesn't seem to be any point.
 */

using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Datum
    {
        public static bool IsTrue(Datum d)
        {
            // Everything not false is true.
            Boolean b = d as Boolean;
            if (b != null && b.val == false)
                return false;
            else
                return true;
        }
        public static Datum List(params Datum[] elts)
        {
            Pair list = null;
            for (int i = elts.Length - 1; i>= 0; i--)
                list = new Pair(elts[i], list);
            return list;
        }
        public Datum Car
        {
            get { return ((Pair)this).car; }
        }

        public Datum Cdr
        {
            get { return ((Pair)this).cdr; }
        }

        public Datum First
        {
            get { return this.Car; }
        }

        public Datum Second
        {
            get { return this.Cdr.Car; }
        }

        public Datum Third
        {
            get { return this.Cdr.Cdr.Car; }
        }
    }

    public class Unspecified : Datum
    {
    }

    // FUTURE: These should be interned.

    public class String : Datum
    {
        public string val;

        public String(string v)
        {
            val = v;
        }
    }

    public class Pair : Datum
    {
        public Datum car;
        public Datum cdr;

        public Pair(Datum a, Datum d)
        {
            car = a; cdr = d;
        }
    }

    // FUTURE: These should be interned too.

    public class Symbol : Datum
    {
        public string name;

        public Symbol(string n)
        {
            name = n;
        }
    }

    public class Number : Datum
    {
    }

    // The Integer class is currently unused except as a token to the
    // type-checking utilities in prims.cs.  I should probably change that,
    // so we wouldn't need this class definition.  For now, I've made the
    // constructor private so we won't construct one of them by accident.
    // To represent an integer, just use a Rational with a denominator of
    // 1.

    public class Integer : Number
    {
        private Integer()
        {
        }
    }

    // A Rational is stored as a numerator and a denominator.  On
    // construction, they're reduced to lowest terms by the Euclidean
    // algorithm.  All this is probably terribly inefficient.

    public class Rational : Number
    {
        private int num;
        private int denom;

        public int Num { get { return num; } }
        public int Denom { get { return denom; } }

        private static int Gcd(int i, int j)
        {
            if (j == 1)
                return 1;

            if (i < 0)
                i *= -1;

            while (i != j)
            {
                if (i > j)
                    i = i - j;
                else
                    j = j - i;
            }

            return i;
        }

        public Rational Reciprocal
        {
            get { return new Rational(denom, num); }
        }

        private void Reduce()
        {
            if (num == 0)
            {
                denom = 1;
                return;
            }

            int gcd = Gcd(num, denom);
            num /= gcd;
            denom /= gcd;
        }

        public Rational(int n, int d)
        {
            if (d == 0)
                throw new MathException("Division by zero!");

            num = n;
            denom = d;
            this.Reduce();
        }
    }

    public class Boolean : Datum
    {
        public bool val;

        public Boolean(bool v)
        {
            val = v;
        }
    }

    // A Continuation simply wraps the evaluator's internal representation
    // of the callstack.  What else would you expect?

    public class Continuation : Datum
    {
        public Evaluator.Action call;

        public Continuation(Evaluator.Action c)
        {
            call = c;
        }
    }

    // A closure has formals, a body, and a lexical environment.  It looks
    // just like every other closure you've seen.

    public class Closure : Datum
    {
        public Environment env;
        public Pair formals;
        public Datum body;

        public Closure(Environment e, Pair f, Datum b)
        {
            env = e;
            formals = f;
            body = b;
        }
    }

    // A Primitive wraps a delegate to a function defined in prims.cs
    // (which see for details).

    public delegate Datum PrimitiveImplementation(List<Datum> args);

    public class Primitive : Datum
    {
        public string name;
        public bool magicEnvironment;
        public PrimitiveImplementation implementation;

        public Primitive(
            string n, PrimitiveImplementation i, bool menv)
        {
            name = n;
            implementation = i;
            magicEnvironment = menv;
        }
    }
}
