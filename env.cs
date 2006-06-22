using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Environment
    {
        public static Environment Toplevel;

        private Environment parent;
        private Dictionary<string, Datum> bindings;

        public Environment(Environment p)
        {
            Shell.Trace("CREATING ENVIRONMENT " +
                        this.GetHashCode().ToString() + "\n");
            parent = p;
            bindings = new Dictionary<string, Datum>();
        }

        public void Bind(Symbol sym, Datum value)
        {
            Shell.Trace("BINDING " + sym.name + "\n   with ");
            Shell.TracePrint(value);
            Shell.Trace("\n   in " + this.GetHashCode().ToString() + "\n");
            bindings[sym.name] = value;
        }

        public Datum Lookup(Symbol sym)
        {
            Environment env = this;
            while (env != null)
            {
                Shell.Trace("LOOKING UP " + sym.name + "\n   in " +
                            env.GetHashCode().ToString() + "\n");
                if (env.bindings.ContainsKey(sym.name))
                {
                    Shell.Trace("FOUND ");
                    Shell.TracePrint(env.bindings[sym.name]);
                    Shell.Trace("\n");
                    return env.bindings[sym.name];
                }
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
