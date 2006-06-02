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
            if (bindings.ContainsKey(sym.name))
                return bindings[sym.name];
            else if (parent != null)
                return parent.Lookup(sym);
            else
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
                                     accum += n.val;
                                 args = (Pair)args.cdr;
                             }
                             return new Number(accum);
                         }));

            return env;
        }
    }
}
