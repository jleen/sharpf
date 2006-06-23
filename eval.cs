using System;
using System.Collections.Generic;

namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        public enum Actor
        {
            Continue,
            Eval,
            EvalList,
            EvalSequence,
            ExecuteApply,
            ExecuteIf,
            ExecuteDefine
        }

        public class Action
        {
            public Actor target;
            public Datum arg;
            public Environment env;
            public Action next;
            public Datum result;

            public Action(Actor t, Datum a, Environment e)
            {
                target = t;
                arg = a;
                env = e;
                next = null;
            }

            public Action(
                Actor t, Datum a, Environment e, Action n)
            {
                target = t;
                arg = a;
                env = e;
                next = n;
            }
        }

        public static Datum Act(Action call)
        {
            while (call.target != Actor.Continue)
            {
                Shell.Trace("Entering ", call.target,
                            "\nwith arg ", call.arg,
                            "\nresult ", call.result,
                            "\nand environment ", call.env.GetHashCode());

                switch (call.target)
                {
                    case Actor.Eval:
                    {
                        Datum exp = call.arg;
                        Environment env = call.env;

                        if (exp is Symbol)
                        {
                            call.target = Actor.Continue;
                            call.arg = env.Lookup((Symbol)exp);
                            goto NextCall;
                        }
                        if (exp is Pair)
                        {
                            Pair form = (Pair)exp;

                            if (form.car is Symbol)
                            {
                                string name = (form.car as Symbol).name;
                                try
                                {
                                    switch (name)
                                    {
                                        case "lambda":
                                        {
                                            Pair formals = (Pair)form.Second;
                                            Datum body = form.Cdr.Cdr;

                                            call.target = Actor.Continue;
                                            call.arg = new Closure(
                                                env, formals, body);
                                            goto NextCall;
                                        }

                                        case "quote":
                                        {
                                            call.target = Actor.Continue;
                                            call.arg = form.Second;
                                            goto NextCall;
                                        }
                                        case "begin":
                                        {
                                            call.target =
                                                Actor.EvalSequence;
                                            call.arg = form.cdr;
                                            goto NextCall;
                                        }
                                        case "if":
                                        {
                                            Datum test = form.Second;
                                            Datum conseq = form.Third;
                                            Datum alts = form.Cdr.Cdr.Cdr;

                                            call.target = Actor.Eval;
                                            call.arg = test;
                                            call.env = env;

                                            call.next = new Action(
                                                Actor.ExecuteIf,
                                                new Pair(conseq, alts),
                                                env,
                                                call.next);

                                            goto NextCall;
                                        }
                                        case "define":
                                        {
                                            Datum what = form.Second;
                                            Symbol who;
                                            if (what is Symbol)
                                            {
                                                who =
                                                    (Symbol)what;
                                                Datum val_exp = form.Third;

                                                call.target = Actor.Eval;
                                                call.arg = val_exp;
                                                call.env = env;
                                            }
                                            else
                                            {
                                                who = (Symbol)what.Car;
                                                Pair formals = (Pair)what.Cdr;
                                                Datum body = form.Cdr.Cdr;

                                                call.target =
                                                    Actor.Continue;
                                                call.arg = new Closure(
                                                    env, formals, body);
                                            }

                                            call.next = new Action(
                                                Actor.ExecuteDefine,
                                                who,
                                                env,
                                                call.next);

                                            goto NextCall;

                                        }
                                    }
                                }
                                catch (NullReferenceException)
                                {
                                    throw new BadFormException(name);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new BadFormException(name);
                                }
                            }

                            call.target = Actor.EvalList;
                            call.arg = new Pair(form, null);
                            call.env = env;
                            
                            call.next = new Action(
                                Actor.ExecuteApply,
                                null,
                                env,
                                call.next);

                            goto NextCall;
                        }
                        else // Self-evaluating
                        {
                            call.target = Actor.Continue;
                            call.arg = exp;
                            goto NextCall;
                        }
                    }

                    case Actor.EvalList:
                    {
                        Pair p = (Pair)call.arg;
                        Pair exps = (Pair)p.car;
                        Pair acc = (Pair)p.cdr;

                        // Cheesiness: First time we're here, we stick a null
                        // in the accumulator.  Yuck.
                        acc = new Pair(call.result, acc);

                        if (exps == null)
                        {
                            call.target = Actor.Continue;
                            call.arg = acc;
                            goto NextCall;
                        }
                        else
                        {
                            call.target = Actor.Eval;
                            call.arg = exps.car;
                            call.next = new Action(
                                Actor.EvalList,
                                new Pair(exps.cdr, acc),
                                call.env,
                                call.next);

                            goto NextCall;
                        }
                    }

                    case Actor.ExecuteApply:
                    {
                        Environment env = call.env;
                        Pair vals = (Pair)call.result;

                        List<Datum> args = new List<Datum>();
                        while (vals != null)
                        {
                            args.Add(vals.car);
                            vals = (Pair)vals.cdr;
                        }
                        args.Reverse();
                        
                        // Remove cheesy null
                        args.RemoveAt(0);

                        Datum func = args[0];
                        args.RemoveAt(0);

                        // Primitive

                        Primitive prim = func as Primitive;
                        if (prim != null)
                        {
                            // ouch
                            if (prim.name == "call-with-current-continuation")
                            {
                                Continuation current =
                                    new Continuation(call.next);
                                Datum recip = args[0];
                                call.target = Actor.Continue;
                                call.arg =
                                    new Pair(current, new Pair(recip, null));
                                call.next = new Action(
                                    Actor.ExecuteApply,
                                    null,
                                    env,
                                    call.next);
                                goto NextCall;
                            }
                            else
                            {
                                call.target = Actor.Continue;
                                call.arg = prim.implementation(args);
                                goto NextCall;
                            }
                        }

                        // Continuation

                        Continuation cont = func as Continuation;
                        if (cont != null)
                        {
                            call.target = Actor.Continue;
                            call.arg = args[0];
                            // We cheerfully blow away the existing value
                            // of call.next, because this is a goto.
                            call.next = cont.call;
                            goto NextCall;
                        }

                        // Closure

                        Closure closure = func as Closure;

                        if (closure != null)
                        {
                            Environment new_env = new Environment(env);
                            Pair paramlist = closure.formals;
                            foreach (Datum arg in args)
                            {
                                new_env.Bind((Symbol)paramlist.car, arg);
                                paramlist = (Pair)paramlist.cdr;
                            }

                            call.target = Actor.EvalSequence;
                            call.arg = closure.body;
                            call.env = new_env;
                            goto NextCall;
                        }

                        // Inapplicable

                        throw new InapplicableException(func);
                    }

                    case Actor.ExecuteIf:
                    {
                        Pair p = (Pair)call.arg;
                        Datum conseq = p.car;
                        Datum alts = p.cdr;

                        Boolean bool_res = call.result as Boolean;
                        if (bool_res != null &&
                            bool_res.val == false)
                        {
                            call.target =
                                Actor.EvalSequence;
                            call.arg = alts;
                            goto NextCall;
                        }
                        else
                        {
                            call.target = Actor.Eval;
                            call.arg = conseq;
                            goto NextCall;
                        }
                    }

                    case Actor.ExecuteDefine:
                    {

                        call.env.Bind((Symbol)call.arg, call.result);

                        call.target = Actor.Continue;
                        call.arg = new Unspecified();
                        goto NextCall;
                    }

                    case Actor.EvalSequence:
                    {
                        if (null == call.arg)
                        {
                            call.target = Actor.Continue;
                            call.arg = new Pair(new Unspecified(), null);
                        }

                        Pair exps = (Pair)call.arg;
                        Environment env = call.env;

                        if (null == exps.cdr)
                        {
                            call.target = Actor.Eval;
                            call.arg = exps.car;
                            call.env = env;
                            goto NextCall;
                        }
                        else
                        {
                            call.target = Actor.Eval;
                            call.arg = exps.car;
                            call.env = env;
                            call.next = new Action(
                                Actor.EvalSequence,
                                exps.cdr,
                                env,
                                call.next);
                            goto NextCall;
                        }
                    }
                }

NextCall:
                if (call.target != Actor.Continue)
                {
                    call.result = null;
                }

                if (call.target == Actor.Continue &&
                    call.next != null)
                {
                    Shell.Trace("Continuing with result ", call.arg);
                    call.next.result = call.arg;
                    call = call.next;
                }
            }

            Shell.Trace("Returning...\n");
            return call.arg;
        }
    }
}
