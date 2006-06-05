namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        public delegate TrampCall TrampTarget(Datum arg, Environment e);

        public class TrampCall
        {
            public TrampTarget target;
            public Datum arg;
            public Environment env;
            public TrampCall next;

            public TrampCall(TrampTarget t, Datum a, Environment e)
            {
                target = t;
                arg = a;
                env = e;
                next = null;
            }

            public TrampCall(
                TrampTarget t, Datum a, Environment e, TrampCall n)
            {
                target = t;
                arg = a;
                env = e;
                next = n;
            }
        }

        public static Datum Trampoline(TrampCall call)
        {
            while (call.target != null)
            {
                call = call.target(call.arg, call.env);
                if (call.target == null &&
                    call.next != null)
                {
                    call = call.next;
                }
            }

            return call.arg;
        }

        public static TrampCall Eval(Datum exp, Environment env)
        {
            if (exp is Symbol)
            {
                return new TrampCall(
                    null,
                    env.Lookup((Symbol)exp),
                    null);
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
                            Pair formals = (Pair)((Pair)p.cdr).car;
                            Datum body = (Pair)((Pair)p.cdr).cdr;
                            return new TrampCall(
                                null,
                                new Closure(env, formals, body),
                                null);
                        }

                        case "quote":
                        {
                            return new TrampCall(
                                null,
                                ((Pair)p.cdr).car,
                                null);
                        }
                        case "begin":
                        {
                            return new TrampCall(
                                EvalSequence,
                                p.cdr,
                                env);
                        }
                        case "define":
                        {
                            Symbol name = (Symbol)((Pair)p.cdr).car;
                            Datum val_exp = ((Pair)((Pair)p.cdr).cdr).car;

                            env.Bind(
                                name,
                                Trampoline(
                                    new TrampCall(Eval, val_exp, env)));
                            return new TrampCall(
                                null,
                                new Unspecified(),
                                null);
                        }
                    }
                }

                Datum func = Trampoline(new TrampCall(Eval, p.car, env));
                Pair args = EvalList((Pair)p.cdr, env);

                Primitive prim = func as Primitive;
                if (prim != null)
                {
                    return new TrampCall(
                        null,
                        prim.implementation(args),
                        null);
                }

                return new TrampCall(
                    Apply,
                    new Pair(func, args),
                    env);
            }
            else
            {
                return new TrampCall(
                    null,
                    exp,
                    null);
            }
        }

        public static TrampCall Apply(Datum d, Environment parent)
        {
            Pair p = (Pair)d;
            Closure func = (Closure)p.car;
            Pair args = (Pair)p.cdr;

            Environment env = new Environment(parent);
            Pair formals = func.formals;
            while (formals != null)
            {
                env.Bind((Symbol)formals.car, args.car);
                args = (Pair)args.cdr;
                formals = (Pair)formals.cdr;
            }
            return new TrampCall(
                EvalSequence,
                func.body,
                env);
        }

        public static Pair EvalList(Pair exps, Environment env)
        {
            if (exps == null)
                return null;
            else
                return new Pair(
                    Trampoline(new TrampCall(Eval, exps.car, env)),
                    EvalList((Pair)exps.cdr, env));
        }

        public static TrampCall EvalSequence(Datum d, Environment env)
        {
            Pair exps = (Pair)d;
            if (null == exps.cdr)
            {
                return new TrampCall(Eval, exps.car, env);
            }
            else
            {
                return new TrampCall(
                    Eval,
                    exps.car,
                    env,
                    new TrampCall(
                        EvalSequence,
                        exps.cdr,
                        env));
            }
        }
    }
}
