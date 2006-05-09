namespace SaturnValley.SharpF
{
    public class Datum
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
}
