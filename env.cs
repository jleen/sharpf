using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Environment : Datum
    {
        public static Environment Toplevel;

        private Environment parent;
        private Dictionary<string, Datum> bindings;

        public Environment(Environment p)
        {
            Shell.Trace("Creating environment " +
                        this.GetHashCode().ToString() + "\n");
            parent = p;
            bindings = new Dictionary<string, Datum>();
        }

        public void Bind(Symbol sym, Datum value)
        {
            Shell.Trace("Binding name ", sym,
                        "\nto value ", value,
                        "\nin environment ", this.GetHashCode());
            bindings[sym.name] = value;
        }

        public Datum Lookup(Symbol sym)
        {
            Environment env = this;
            while (env != null)
            {
                Shell.Trace("Looking up name ", sym,
                            "\nin environment ", env.GetHashCode());
                if (env.bindings.ContainsKey(sym.name))
                {
                    Shell.Trace("Found value ", env.bindings[sym.name]);
                    return env.bindings[sym.name];
                }
                env = env.parent;
            }

            throw new UnboundSymbolException(sym);
        }

        public static Environment CreateDefaultEnvironment()
        {
            Environment env = new Environment(null);
            Primitives.BindPrimitives(env);
            return env;
        }
    }
}
