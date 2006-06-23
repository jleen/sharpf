using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Datum
    {
    }

    public class Unspecified : Datum
    {
    }

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

    public class Integer : Number
    {
        public int val;

        public Integer(int v)
        {
            val = v;
        }
    }

    public class Rational : Number
    {
        private int num;
        private int denom;

        public int Num { get { return num; } }
        public int Denom { get { return denom; } }

        private static int Gcd(int i, int j)
        {
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

        public void Reciprocal()
        {
            int swap = num;
            denom = num;
            num = denom;
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

    public class Continuation : Datum
    {
        public Evaluator.Action call;

        public Continuation(Evaluator.Action c)
        {
            call = c;
        }
    }

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

    public delegate Datum PrimitiveImplementation(List<Datum> args);

    public class Primitive : Datum
    {
        public string name;
        public PrimitiveImplementation implementation;

        public Primitive(string n, PrimitiveImplementation i)
        {
            name = n;
            implementation = i;
        }
    }
}
