using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        public enum TrampTarget
        {
            Continue,
            Eval,
            EvalSequence,
            ExecuteApply,
            ExecuteIf,
            ExecuteDefine
        }

        public class TrampCall
        {
            public TrampTarget target;
            public Datum arg;
            public Environment env;
            public TrampCall next;
            public Datum result;

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
                Shell.Trace(call.target + ": ");
                Shell.TracePrint(call.arg);
                Shell.Trace("\n");

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

                                        call.target = TrampTarget.Eval;
                                        call.arg = test;
                                        call.env = env;

                                        call.next = new TrampCall(
                                            TrampTarget.ExecuteIf,
                                            new Pair(conseq, alts),
                                            env,
                                            call.next);

                                        goto NextCall;
                                    }
                                    case "define":
                                    {
                                        Symbol name =
                                            (Symbol)((Pair)p.cdr).car;
                                        Datum val_exp =
                                            ((Pair)((Pair)p.cdr).cdr).car;

                                        call.target = TrampTarget.Eval;
                                        call.arg = val_exp;
                                        call.env = env;

                                        call.next = new TrampCall(
                                            TrampTarget.ExecuteDefine,
                                            name,
                                            env,
                                            call.next);

                                        goto NextCall;
                                    }
                                }
                            }

                            call.target = TrampTarget.Eval;
                            call.arg = p.car;
                            call.env = env;
                            
                            call.next = new TrampCall(
                                TrampTarget.ExecuteApply,
                                p.cdr,
                                env);

                            goto NextCall;
                        }
                        else // Self-evaluating
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = exp;
                            goto NextCall;
                        }
                    }

                    case TrampTarget.ExecuteApply:
                    {
                        Environment env = call.env;
                        List<Datum> args = EvalList((Pair)call.arg, env);

                        Primitive prim = call.result as Primitive;
                        if (prim != null)
                        {
                            call.target = TrampTarget.Continue;
                            call.arg = prim.implementation(args);
                            goto NextCall;
                        }

                        // Apply

                        Closure closure = (Closure)call.result;

                        Environment new_env = new Environment(env);
                        Pair paramlist = closure.formals;
                        foreach (Datum arg in args)
                        {
                            new_env.Bind((Symbol)paramlist.car, arg);
                            paramlist = (Pair)paramlist.cdr;
                        }

                        call.target = TrampTarget.EvalSequence;
                        call.arg = closure.body;
                        call.env = new_env;
                        goto NextCall;
                    }

                    case TrampTarget.ExecuteIf:
                    {
                        Pair p = (Pair)call.arg;
                        Datum conseq = p.car;
                        Datum alts = p.cdr;

                        Boolean bool_res = call.result as Boolean;
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

                    case TrampTarget.ExecuteDefine:
                    {

                        call.env.Bind((Symbol)call.arg, call.result);

                        call.target = TrampTarget.Continue;
                        call.arg = new Unspecified();
                        goto NextCall;
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
                }

NextCall:
                if (call.target != TrampTarget.Continue)
                {
                    call.result = null;
                }

                if (call.target == TrampTarget.Continue &&
                    call.next != null)
                {
                    Shell.Trace("Continuing...");
                    call.next.result = call.arg;
                    call = call.next;
                }
            }

            Shell.Trace("Returning...");
            return call.arg;
        }

        private static List<Datum> EvalList(Pair exps, Environment env)
        {
            List<Datum> vals = new List<Datum>();

            while (exps != null)
            {
                vals.Add(Trampoline(new TrampCall(
                    TrampTarget.Eval, exps.car, env)));
                exps = (Pair)exps.cdr;
            }

            return vals;
        }
    }
}
