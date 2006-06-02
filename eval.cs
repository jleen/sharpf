namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        public static Datum Eval(Datum exp, Environment env)
        {
            if (exp is Symbol)
            {
                return env.Lookup((Symbol)exp);
            }
            if (exp is Pair)
            {
                Pair p = (Pair)exp;
                
                if (p.car is Symbol)
                {
                    switch ((p.car as Symbol).name)
                    {
                        case "lambda":
                        {
                            System.Console.WriteLine("lambda");
                            Pair formals = (Pair)((Pair)p.cdr).car;
                            Datum body = ((Pair)((Pair)p.cdr).cdr).car;
                            return new Closure(env, formals, body);
                        }
                    }
                }

                Datum func = Eval(p.car, env);
                Pair args = EvalList((Pair)p.cdr, env);

                Primitive prim = func as Primitive;
                if (prim != null)
                    return prim.implementation(args);

                return Apply((Closure)func, args, env);
            }
            else
            {
                return exp;
            }
        }

        public static Datum Apply(Closure func, Pair args, Environment parent)
        {
            Environment env = new Environment(parent);
            Pair formals = func.formals;
            while (formals != null)
            {
                env.Bind((Symbol)formals.car, args.car);
                args = (Pair)args.cdr;
                formals = (Pair)formals.cdr;
            }
            return Eval(func.body, env);
        }

        public static Pair EvalList(Pair exps, Environment env)
        {
            if (exps == null)
                return null;
            else
                return new Pair(Eval(exps.car, env),
                                EvalList((Pair)exps.cdr, env));
        }
    }
}
