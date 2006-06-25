/*
 * env.cs:
 *
 * An environment is more or less a stack frame, or a linked list of stack
 * frames.  We implement our Environments with CLR hashtables.  Why not.
 *
 * Environments are first-class Scheme objects and as such they technically
 * belong in data.cs, but they're complex enough that they seem to deserve
 * their own file.
 */

using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public class Environment : Datum
    {
        // A distinguished Environment used by the REPL.

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

        // Bind a value in the current environment.

        public void Bind(Symbol sym, Datum value)
        {
            Shell.Trace("Binding name ", sym,
                        "\nto value ", value,
                        "\nin environment ", this.GetHashCode());
            bindings[sym.name] = value;
        }

        // Assign a new value to a name already bound in the current or an
        // enclosing environment.

        public void Set(Symbol sym, Datum value)
        {
            Environment env = this;
            while (env != null)
            {
                if (env.bindings.ContainsKey(sym.name))
                {
                    env.bindings[sym.name] = value;
                    return;
                }
                env = env.parent;
            }

            throw new UnboundSymbolException(sym);
        }

        // Recursively look up a value in the current or an enclosing
        // environment.

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

        // Return a fresh environment with the default primitive bindings,
        // suitable for the REPL toplevel.

        public static Environment CreateDefaultEnvironment()
        {
            Environment env = new Environment(null);
            Primitives.BindPrimitives(env);
            return env;
        }
    }
}
