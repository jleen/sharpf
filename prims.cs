using System.IO;

namespace SaturnValley.SharpF
{
    static class Primitives
    {
        public static Datum Load(string filename, Environment env)
        {
            Datum d = new Unspecified();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() != -1)
                    d = Evaluator.Trampoline(
                        new Evaluator.TrampCall(
                            Evaluator.TrampTarget.Eval,
                            Shell.Read(sr),
                            env));
            }
            return d;
        }

        public static void BindPrimitives(Environment e)
        {
            e.Bind(
                new Symbol("+"),
                new Primitive(delegate(Pair args, Environment env)
                {
                    int accum = 0;
                    while (args != null)
                    {
                        Number n = args.car as Number;
                        if (n != null)
                            accum += n.val;
                        args = (Pair)args.cdr;
                    }
                    return new Number(accum);
                }));
            e.Bind(
                new Symbol("car"),
                new Primitive(delegate(Pair args, Environment env)
                    {
                        return ((Pair)(args.car)).car;
                    }));
            e.Bind(
                new Symbol("cdr"),
                new Primitive(delegate(Pair args, Environment env)
                    {
                        return ((Pair)(args.car)).cdr;
                    }));
            e.Bind(
                new Symbol("load"),
                new Primitive(delegate(Pair args, Environment env)
                    {
                        string filename = ((String)args.car).val;
                        return Load(filename, env);
                    }));
        }
    }
}
