using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Environment
    {
        private Environment parent;
        private Dictionary<string, Datum> bindings;

        public Environment(Environment p)
        {
            parent = p;
            bindings = new Dictionary<string, Datum>();
        }

        public void Bind(Symbol sym, Datum value)
        {
            bindings[sym.name] = value;
        }

        public Datum Lookup(Symbol sym)
        {
            Environment env = this;
            while (env != null)
            {
                if (env.bindings.ContainsKey(sym.name))
                    return env.bindings[sym.name];
                env = env.parent;
            }

            throw new System.Exception("Unable to look up " + sym.name);
        }

        public static Environment CreateDefaultEnvironment()
        {
            Environment env = new Environment(null);

            env.Bind(new Symbol("+"),
                     new Primitive(
                         delegate(Pair args)
                         {
                             int accum = 0;
                             while (args != null)
                             {
                                 Number n = args.car as Number;
                                 if (n != null)
                                 {
                                     accum += n.val;
                                     if (n.val == 10000)
                                        System.Console.WriteLine("10000!");
                                 }
                                 args = (Pair)args.cdr;
                             }
                             return new Number(accum);
                         }));

            return env;
        }
    }
}
