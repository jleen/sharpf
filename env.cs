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
            Primitives.BindPrimitives(env);
            return env;
        }
    }
}
