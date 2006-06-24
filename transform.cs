using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public static class Transform
    {
        struct LetComps
        {
            public List<Pair> bindings;
            public Datum body;
        }
            
        private static LetComps DestructureLet(Datum exp)
        {
            LetComps comps;
            comps.bindings = new List<Pair>();
            Datum bindList = exp.Second;
            while (bindList != null)
            {
                comps.bindings.Add((Pair)bindList.Car);
                bindList = bindList.Cdr;
            }
            comps.body = exp.Cdr.Cdr;
            return comps;
        }

        private static Datum ConstructLambdaFromLet(LetComps comps)
        {
            // unzip
            List<Datum> names = new List<Datum>();
            List<Datum> vals = new List<Datum>();
            foreach (Pair p in comps.bindings)
            {
                names.Add(p.First);
                vals.Add(p.Second);
            }
            Datum formals = Primitives.List(names);
            Datum transform = 
                new Pair(new Pair(new Symbol("lambda"),
                                  new Pair(formals, comps.body)),
                         Primitives.List(vals));
            Shell.Trace("LET transform produced ", transform);
            return transform;
        }

        public static Datum Let(Datum exp)
        {
            return ConstructLambdaFromLet(DestructureLet(exp));
        }

        private static Datum ConstructLambdaFromLetStar(LetComps comps)
        {
            LetComps outer;
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

        public static Datum LetStar(Datum exp)
        {
            return ConstructLambdaFromLetStar(DestructureLet(exp));
        }
    }
}
