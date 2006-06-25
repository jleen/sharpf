/*
 * eval.cs:
 *
 * The fun part.  The evaluator is written in pure continuation-passing
 * style.  All calls are tail calls.  But C# doesn't optimize tail calls,
 * so a naive implementation would rapidly blow the stack.  We avoid this
 * grisly fate by staying off the CLR stack, which is to say, we avoid
 * C# function calls.  We accomplish this ninjutsu by writing the evaluator
 * as a loop, with a linked list of Action objects serving as our control
 * stack.  On each trip through the loop, the head of the Action list tells
 * us what to do.  When we're done, we can either "return" by cdring down
 * the list, or we can "call" another function either by consing it onto
 * the list or blasting it in with set-car! for a tail call.  I'm speaking
 * metaphorically, of course.  We do this all with C# constructs.
 *
 * Problem: continuation-passing style would seem not to work very well
 * without closures.  C# kind of sort of has closures, but they won't help
 * us here since we're not calling functions.  Instead, each Action on the
 * continuation chain holds a token indicating a branch of the loop; an
 * "arg" which is bound at the time we place the Action in the chain; and a
 * "result" which is bound by the caller who ends up preceding this Action.
 *
 * Here is our calling convention:
 *
 *   - Instead of functions, we have "actors".  An actor is a case in the
 *     big switch block inside Evaluator.Act.  The Actor enum controls
 *     dispatch to actors.
 *
 *   - There is a variable "call" which is in scope outside the main loop,
 *     and is thus for all intents and purposes global.  Like any global,
 *     it should be considered mildly radioactive.  Avoid touching it,
 *     except...
 *
 *   - Each actor should begin by retrieving its arg and result from call,
 *     and not look at call thereafter.  I'm not very dilligent about this.
 *     Sue me.
 *
 *   - To make a tail call, an actor should assign a new arg, result, and
 *     target to call, then immediately goto NextCall.
 *
 *   - To make a non-tail call... well, in CPS you can't really make a
 *     non-tail call.  Control will never, ever return to this invocation
 *     of this actor.  But you can pass a continuation whose sole purpose
 *     is to pick up what you were doing.  Just be sure that *its*
 *     continuation is your current continuation, or you'll "break the
 *     chain" and control will never get back to what you were supposed to
 *     be doing in the first place.  This is deep.  (Deeper still:
 *     calling a continuation deliberately breaks the chain.  That's what
 *     it's for.)
 *
 * Note that we never exit the switch block with anything so prosaic as a
 * break.  We always goto NextCall.  That's the calling convention.
 * Further refinements:
 *
 *   - We never explicitly cdr down the list (i.e. "pop the stack").
 *     Instead, we request it by assigning Continue as our next actor.
 *     This keeps things a little better-controlled.
 *
 *   - Similarly, we never assign call.result directly.  Instead, Continue
 *     transfers arg to result.  This controls access to result, and also
 *     ensures that we'll only ever assign the result of the frame just
 *     about to be activated.  So the only two times we can pass an
 *     argument to a call are (1) when it's first placed in the
 *     continuation chain, and (2) when it's finally being activated.
 *     These cases correspond, respectively, to defining a lambda in a
 *     lexical scope with bound variables, and finally calling the lambda
 *     and passing it its invocation arguments.
 *
 * And now, without further ado...
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SaturnValley.SharpF
{
    public static class Evaluator
    {
        // Dispatch tokens.

        public enum Actor
        {
            Continue,
            Apply,
            Eval,
            EvalList,
            EvalSequence,
            ExecuteApply,
            ExecuteIf,
            ExecuteCond,
            ExecuteSetQ,
            ExecuteDefine
        }

        // A stack frame.  In other words, a link in our linked list of
        // actions to take when done with the current one.  In other words,
        // a continuation.

        public class Action
        {
            // Dispatch token for the actor to invoke.
            public Actor target;

            // "Curried" argument bound at the time this action is placed
            // in the chain.
            public Datum arg;

            // Environment in which to invoke the actor.  Also assigned
            // when we're placed in the chain.
            public Environment env;

            // The next link of the chain.  The continuation's
            // continuation.  Assigned, again, at enqueue.
            public Action next;

            // The immediate argument to this invocation.  Only assigned by
            // the Continue actor.  We're a little paranoid about
            // controlling read access to it, because not everyone assigns
            // it and not everyone checks it, and we don't want any
            // screw-ups.

            private Datum result;
            public bool hasResult;

            public Datum Result
            {
                get
                {
                    Debug.Assert(hasResult);
                    return result;
                }
                set
                {
                    result = value;
                    hasResult = true;
                }
            }

            public bool HasResult
            {
                get { return hasResult; }
            }

            public void ClearResult()
            {
                hasResult = false;
            }

            // The constructor does about what you'd expect.  Note that,
            // true to our word, we don't let you assign a result here.

            public Action(
                Actor t, Datum a, Environment e, Action n)
            {
                target = t;
                arg = a;
                env = e;
                next = n;
                result = null;
                hasResult = false;
            }
        }

        // Act is the only entry point into the evaluator.  It requires
        // that we already have a continuation.  (I suggest cooking up one
        // that invokes Eval.)  This whole function is one big loop.  CPS
        // turns recursion into tail recursion, and our calling convention
        // turns tail recursion into iteration.  BEHOLD!

        public static Datum Act(Action call)
        {
            while (call.target != Actor.Continue)
            {
                Shell.TraceAction("Entering", call);
                switch (call.target)
                {
                    // Evaluate a single form.  There's a case for each
                    // sort of form.

                    case Actor.Eval:
                    {
                        Datum exp = call.arg;
                        Environment env = call.env;

                        if (exp is Symbol)
                        {
                            // Symbol.  Look up the value.

                            call.target = Actor.Continue;
                            call.arg = env.Lookup((Symbol)exp);
                            goto NextCall;
                        }
                        if (exp is Pair)
                        {
                            // A list evaluates as a function call, unless
                            // it's a special form.

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
                                            // Evaluates to a closure.

                                            Pair formals = (Pair)form.Second;
                                            Datum body = form.Cdr.Cdr;

                                            call.target = Actor.Continue;
                                            call.arg = new Closure(
                                                env, formals, body);
                                            goto NextCall;
                                        }

                                        case "quote":
                                        {
                                            // Evaluates to its
                                            // (unevaluated) argument.

                                            call.target = Actor.Continue;
                                            call.arg = form.Second;
                                            goto NextCall;
                                        }

                                        case "begin":
                                        {
                                            // Evaluates to its last
                                            // argument, after evaluating
                                            // the others in sequence for
                                            // their side-effects.

                                            call.target =
                                                Actor.EvalSequence;
                                            call.arg = form.cdr;
                                            goto NextCall;
                                        }

                                        case "if":
                                        {
                                            // It looks exactly like every
                                            // other IF you've ever seen,
                                            // except that this one is CPS
                                            // so all we do here is enqueue
                                            // evaluation of the test
                                            // expression followed by an
                                            // actor which will branch to
                                            // the then or else clause.

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

                                        case "cond":
                                        {
                                            // COND is sneaky and will live
                                            // entirely in its own actor.

                                            Datum clauses = form.Cdr;

                                            call.target = Actor.ExecuteCond;
                                            call.arg = new Pair(clauses, null);
                                            call.env = env;
                                            goto NextCall;
                                        }

                                        case "set!":
                                        {
                                            // Assignment.  Enqueue
                                            // evaluation of the value
                                            // expression, followed by a
                                            // helper actor that does the
                                            // assignment.

                                            Symbol what = (Symbol)form.Second;
                                            Datum val_exp = form.Third;

                                            call.target = Actor.Eval;
                                            call.arg = val_exp;
                                            call.next = new Action(
                                                Actor.ExecuteSetQ,
                                                what,
                                                env,
                                                call.next);
                                            goto NextCall;
                                        }

                                        case "define":
                                        {
                                            // Bind a new symbol in the
                                            // current environment.  Has a
                                            // helper actor.

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

                                        case "let":
                                        {
                                            // A macro.  Perform the
                                            // transformation and evaluate
                                            // the resulting expression.  A
                                            // beautiful case for tail
                                            // calls.

                                            call.target = Actor.Eval;
                                            call.arg =
                                                Transform.Let(form);
                                            goto NextCall;
                                        }

                                        case "let*":
                                        {
                                            // Another macro.

                                            call.target = Actor.Eval;
                                            call.arg =
                                                Transform.LetStar(form);
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

                            // If we've gotten here, we aren't a special
                            // form, so our list is a function application.
                            // We'll evaluate each element of the list and
                            // then feed the cdr to the car.  I've just
                            // mentioned two actions, and sure enough,
                            // we've about to enqueue them both...

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
                        else
                        {
                            // All other data types (strings, numbers, &c)
                            // are self-evaluating.
                            call.target = Actor.Continue;
                            call.arg = exp;
                            goto NextCall;
                        }
                    }

                    // Evaluate a list of forms, in sequence.

                    case Actor.EvalList:
                    {
                        Pair p = (Pair)call.arg;
                        Pair exps = (Pair)p.car;
                        Pair acc = (Pair)p.cdr;

                        if (call.HasResult)
                            acc = new Pair(call.Result, acc);

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

                    // Receive the product of EvalList and feed it to
                    // Apply.  We need this glue so that we can also invoke
                    // Apply normally, without a result.

                    case Actor.ExecuteApply:
                    {
                        call.target = Actor.Apply;
                        call.arg = call.Result;
                        goto NextCall;
                    }

                    // Apply a function to its arguments.  Ugly kludge: the
                    // argument list is backwards, with the function itself
                    // last.  Sorry.  It was easier to generate this way.

                    case Actor.Apply:
                    {
                        Environment env = call.env;
                        Pair vals = (Pair)call.arg;

                        // Juggle the backwards linked list.

                        List<Datum> args = new List<Datum>();
                        while (vals != null)
                        {
                            args.Add(vals.car);
                            vals = (Pair)vals.cdr;
                        }
                        args.Reverse();
                        
                        Datum func = args[0];
                        args.RemoveAt(0);

                        // Apply a primitive procedure: call the
                        // implementation delegate.

                        Primitive prim = func as Primitive;
                        if (prim != null)
                        {
                            // Ouch.  CALL/CC's implementation has to be
                            // "inline" so that it won't use the CLR stack
                            // and violate CPS.  It's okay for all the
                            // other primitives to do so, because they'll
                            // return immediately and are "conceptually
                            // inline", if you will.  But since CALL/CC
                            // may not return normally, it can't get away
                            // with being on the CLR stack.
                            //
                            // For all that, the actual implementation is
                            // trivial: package up the Action chain into a
                            // Scheme Continuation object, and apply the
                            // argument to it.

                            if (prim.name == "call-with-current-continuation")
                            {
                                Continuation current =
                                    new Continuation(
                                        new Action(
                                            call.next.target,
                                            call.next.arg,
                                            call.next.env,
                                            call.next.next));
                                Shell.TraceAction(
                                    "Creating continuation which will",
                                    call.next);
                                Datum recip = args[0];
                                call.target = Actor.Apply;
                                call.arg =
                                    new Pair(current, new Pair(recip, null));
                                goto NextCall;
                            }
                            else
                            {
                                // If it's not CALL/CC, just call the
                                // delegate, passing an implicit argument
                                // if there is one.

                                call.target = Actor.Continue;
                                if (prim.magicEnvironment)
                                {
                                    args.Insert(0, env);
                                }
                                call.arg = prim.implementation(args);
                                goto NextCall;
                            }
                        }

                        // Apply a continuation.  Delightfully simple.

                        Continuation cont = func as Continuation;
                        if (cont != null)
                        {
                            Shell.TraceAction(
                                "Invoking continuation which will",
                                cont.call);
                            call.target = Actor.Continue;
                            call.arg = args[0];
                            // We cheerfully blow away the existing value
                            // of call.next, because this is a Scheme goto.
                            // This is the place where we "break the
                            // chain".
                            call.next = cont.call;
                            goto NextCall;
                        }

                        // Apply a closure.  If you've read the wizard
                        // book, you can recite the mantra in your sleep:
                        // extend the defining environment, bind the params
                        // to the formals in the new environment, then
                        // evaluate the body in the new environment...

                        Closure closure = func as Closure;

                        if (closure != null)
                        {
                            Environment new_env = new Environment(closure.env);
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

                        // Well, if we're still here, it means some jerk
                        // has asked us to apply a non-function.

                        throw new InapplicableException(func);
                    }

                    // Continue evaluating an IF.  We end up here after
                    // evaluating the test.  We branch to the appropriate
                    // result clause.

                    case Actor.ExecuteIf:
                    {
                        Pair p = (Pair)call.arg;
                        Datum conseq = p.car;
                        Datum alts = p.cdr;

                        if (!Datum.IsTrue(call.Result))
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

                    // Evaluate a COND.  Just keep cdring down the list
                    // until a clause matches.  We'll do lots of two-action
                    // calls to keep coming back here after evaluating a
                    // test.  If there's a result waiting for us, it means
                    // this isn't our first trip through the loop and we
                    // need to check if it's time yet to branch to the
                    // consequent.

                    case Actor.ExecuteCond:
                    {
                        Datum clauses = call.arg.Car;
                        Datum thisConsequent = call.arg.Cdr;
                        Environment env = call.env;

                        // If we've just executed a successful test,
                        // continue with the consequent.
                        if (call.HasResult &&
                            Datum.IsTrue(call.Result))
                        {
                            call.target = Actor.Eval;
                            call.arg = thisConsequent;
                            call.env = env;
                            goto NextCall;
                        }

                        // Are we done?
                        if (clauses == null)
                        {
                            // Didn't match any clauses.
                            call.target = Actor.Continue;
                            call.arg = new Unspecified();
                            goto NextCall;
                        }

                        Datum test = clauses.Car.First;
                        Datum consequent = clauses.Car.Second;

                        // Are we at an ELSE?
                        Symbol symTest = test as Symbol;
                        if (symTest != null &&
                            symTest.name == "else")
                        {
                            call.target = Actor.Eval;
                            call.arg = consequent;
                            call.env = env;
                            goto NextCall;
                        }

                        // We've got more clauses.  Do the test, and reenter.
                        call.target = Actor.Eval;
                        call.arg = test;
                        call.env = env;
                        call.next = new Action(
                            Actor.ExecuteCond,
                            new Pair(clauses.Cdr, consequent),
                            env,
                            call.next);
                        goto NextCall;
                    }

                    // Continue assignment.  We've just evaluated the
                    // value, so assign it to the curried name.

                    case Actor.ExecuteSetQ:
                    {
                        call.env.Set((Symbol)call.arg, call.Result);

                        call.target = Actor.Continue;
                        call.arg = new Unspecified();
                        goto NextCall;
                    }

                    // Similarly, continue definition by binding the
                    // result expression to the curried name.

                    case Actor.ExecuteDefine:
                    {

                        call.env.Bind((Symbol)call.arg, call.Result);

                        call.target = Actor.Continue;
                        call.arg = new Unspecified();
                        goto NextCall;
                    }

                    // Evaluate multiple expressions in order, discarding
                    // all but the last result.

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
                // Ahh, the fabled land of NextCall, whither go all actors
                // when their part is played out.  Here we process the
                // Continue actor by shuffling arg and result.  We cdr down
                // the action chain and then finish the body of the while,
                // returning back to the very top of Evaluator.Act to begin
                // the cycle again.

                if (call.target != Actor.Continue)
                {
                    call.ClearResult();
                }

                if (call.target == Actor.Continue &&
                    call.next != null)
                {
                    Shell.Trace("Continuing with result ", call.arg);
                    call.next.Result = call.arg;
                    call = call.next;
                }
            }

            // Well, we've managed to exit the great cosmic while loop.
            // Ths means that we hit an Actor.Continue token which didn't
            // have a next; in other words, the last actor told us to
            // continue but there's nothing to continue doing.  This means
            // we've finally finished performing the act that we were
            // originally called for, "called" in (for once!) the prosaic
            // C# sense.  And so... we return!  Actually, honest-to-gosh
            // return, pop the CLR stack, bid a fond farewell to CPS-land.
            // Sayonara!

            Shell.Trace("Returning...");
            return call.arg;
        }
    }
}
