/*
 * transform.cs:
 * 
 * Hard-coded Common Lisp-style macros for "library syntax".  Scheme
 * contains many special forms which could be implemented with DEFMACRO or
 * DEFINE-SYNTAX, had I implemented DEFMACRO or DEFINE-SYNTAX.  Since I
 * haven't implemented a transformation language, I here use C# as my
 * (rather verbose) transformation language.  Public methods implement
 * transformers for specific special forms.  Private methods are utilities.
 */

using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public static class Transform
    {
        // FUTURE: It might be nice to do attribute-based dispatch to
        // these, as in prims.cs.

        public static Datum Let(Datum exp)
        {
            return ConstructLambdaFromLet(DestructureLet(exp));
        }

        public static Datum LetStar(Datum exp)
        {
            return ConstructLambdaFromLetStar(DestructureLet(exp));
        }

        // This struct is rather "heavy" for what I want to do with it, but
        // it seems the cleanest alternative to multiple return values,
        // which C# lacks.

        private struct LetComps
        {
            public Symbol self;
            public List<Pair> bindings;
            public Datum body;
        }
            
        // Rip up a LET or LET* into its components.

        private static LetComps DestructureLet(Datum exp)
        {
            LetComps comps;
            comps.self = null;
            comps.bindings = new List<Pair>();
            Datum bindList = exp.Second;
            comps.body = exp.Cdr.Cdr;
            if (bindList is Symbol)
            {
                comps.self = (Symbol)bindList;
                bindList = exp.Third;
                comps.body = exp.Cdr.Cdr.Cdr;
            }

            while (bindList != null)
            {
                comps.bindings.Add((Pair)bindList.Car);
                bindList = bindList.Cdr;
            }
            return comps;
        }

        // Take a bag of LET components and write the equivalent LAMBDA
        // expression.  Handles named LET.

        private static Datum ConstructLambdaFromLet(LetComps comps)
        {
            // Unzip!
            List<Datum> names = new List<Datum>();
            List<Datum> vals = new List<Datum>();
            foreach (Pair p in comps.bindings)
            {
                names.Add(p.First);
                vals.Add(p.Second);
            }
            Datum formals = Primitives.List(names);
            Datum bodyFunc = new Pair(new Symbol("lambda"),
                                      new Pair(formals, comps.body));
            Datum transform;
            if (comps.self == null)
            {
                // Unnamed LET.
                transform = new Pair(bodyFunc, Primitives.List(vals));
            }
            else
            {
                // Named LET.
                transform =
                    new Pair(Datum.List(new Symbol("let"),
                                        Datum.List(Datum.List(comps.self,
                                                              null)),
                                        Datum.List(new Symbol("set!"),
                                                   comps.self,
                                                   bodyFunc),
                                        comps.self),
                             Primitives.List(vals));
            }
            Shell.Trace("LET transform produced ", transform);
            return transform;
        }

        // Ditto, but for LET*, which evaluates the bindings sequentially,
        // each visible to its successors.

        private static Datum ConstructLambdaFromLetStar(LetComps comps)
        {
            LetComps outer;
            outer.self = null;
            outer.bindings = new List<Pair>();

            if (comps.bindings.Count == 0)
            {
                outer.body = comps.body;
            }
            else
            {
                Pair firstBinding = comps.bindings[0];
                comps.bindings.RemoveAt(0);

                LetComps inner;
                inner.self = null;
                inner.bindings = comps.bindings;
                inner.body = comps.body;

                outer.bindings.Add(firstBinding);
                // A body is a *list* of expressions.
                outer.body =
                    new Pair(ConstructLambdaFromLetStar(inner),
                             null);
            }
            Datum transform = ConstructLambdaFromLet(outer);
            Shell.Trace("LET* transform produced ", transform);
            return transform;
        }
    }
}
