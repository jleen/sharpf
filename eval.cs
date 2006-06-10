namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        public enum TrampTarget
        {
            Continue,
            Eval,
            EvalSequence,
            EvalList,
            Apply
        }

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
            while (call.target != TrampTarget.Continue)
            {
                switch (call.target)
                {
                    case TrampTarget.Eval:
                    {
                        Datum exp = call.arg;
                        Environment env = call.env;

                        if (exp is Symbol)
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = env.Lookup((Symbol)exp);
                            goto NextCall;
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
                                        call.target = TrampTarget.Continue;
                                        call.arg =
                                            new Closure(env, formals, body);
                                        goto NextCall;
                                    }

                                    case "quote":
                                    {
                                        call.target = TrampTarget.Continue;
                                        call.arg = ((Pair)p.cdr).car;
                                        goto NextCall;
                                    }
                                    case "begin":
                                    {
                                        call.target =
                                            TrampTarget.EvalSequence;
                                        call.arg = p.cdr;
                                        goto NextCall;
                                    }
                                    case "if":
                                    {
                                        Pair clauses = p.cdr as Pair;
                                        Datum test = clauses.car;
                                        Datum conseq = ((Pair)clauses.cdr).car;
                                        Datum alts = ((Pair)clauses.cdr).cdr;

                                        Datum result =
                                            Trampoline(new TrampCall(
                                                TrampTarget.Eval,
                                                test, env));

                                        Boolean bool_res = result as Boolean;
                                        if (bool_res != null &&
                                            bool_res.val == false)
                                        {
                                            call.target =
                                                TrampTarget.EvalSequence;
                                            call.arg = alts;
                                            goto NextCall;
                                        }
                                        else
                                        {
                                            call.target = TrampTarget.Eval;
                                            call.arg = conseq;
                                            goto NextCall;
                                        }
                                    }
                                    case "define":
                                    {
                                        Symbol name =
                                            (Symbol)((Pair)p.cdr).car;
                                        Datum val_exp =
                                            ((Pair)((Pair)p.cdr).cdr).car;

                                        env.Bind(
                                            name,
                                            Trampoline(new TrampCall(
                                                TrampTarget.Eval,
                                                val_exp, env)));
                                        call.target = TrampTarget.Continue;
                                        call.arg = new Unspecified();
                                        goto NextCall;
                                    }
                                }
                            }

                            Datum func =
                                Trampoline(new TrampCall(
                                    TrampTarget.Eval, p.car, env));
                            Pair args =
                                (Pair)Trampoline(new TrampCall(
                                    TrampTarget.EvalList,
                                    p.cdr, env));

                            Primitive prim = func as Primitive;
                            if (prim != null)
                            {
                                call.target = TrampTarget.Continue;
                                call.arg = prim.implementation(args, env);
                                goto NextCall;
                            }

                            call.target = TrampTarget.Apply;
                            call.arg = new Pair(func, args);
                            goto NextCall;
                        }
                        else
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = exp;
                            goto NextCall;
                        }
                    }
                    
                    case TrampTarget.EvalSequence:
                    {
                        if (null == call.arg)
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = new Pair(new Unspecified(), null);
                        }

                        Pair exps = (Pair)call.arg;
                        Environment env = call.env;

                        if (null == exps.cdr)
                        {
                            call.target = TrampTarget.Eval;
                            call.arg = exps.car;
                            call.env = env;
                            goto NextCall;
                        }
                        else
                        {
                            call.target = TrampTarget.Eval;
                            call.arg = exps.car;
                            call.env = env;
                            call.next = new TrampCall(
                                TrampTarget.EvalSequence, exps.cdr, env);
                            goto NextCall;
                        }
                    }

                    case TrampTarget.Apply:
                    {
                        Pair p = (Pair)call.arg;
                        Environment parent = call.env;

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

                        call.target = TrampTarget.EvalSequence;
                        call.arg = func.body;
                        call.env = env;
                        goto NextCall;
                    }

                    case TrampTarget.EvalList:
                    {
                        Pair exps = (Pair)call.arg;
                        Environment env = call.env;

                        if (exps == null)
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = null;
                            goto NextCall;
                        }
                        else
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = new Pair(
                                Trampoline(new TrampCall(
                                    TrampTarget.Eval, exps.car, env)),
                                Trampoline(new TrampCall(
                                    TrampTarget.EvalList, exps.cdr, env)));
                            goto NextCall;
                        }
                    }
                }

            NextCall:
                if (call.target == TrampTarget.Continue &&
                    call.next != null)
                {
                    call = call.next;
                }
            }

            return call.arg;
        }
    }
}
