namespace SaturnValley.SharpF
{
    public class Datum
    {
    }

    public class Unspecified : Datum
    {
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
        public int val;

        public Number(int v)
        {
            val = v;
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

    public delegate Datum PrimitiveImplementation(Pair args);

    public class Primitive : Datum
    {
        public PrimitiveImplementation implementation;

        public Primitive(PrimitiveImplementation i)
        {
            implementation = i;
        }
    }
}
