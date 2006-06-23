using System;

namespace SaturnValley.SharpF
{
    abstract class SharpFException : Exception
    {
    }

    abstract class ParserException : SharpFException
    {
    }

    class TokenException : ParserException
    {
        private string text;

        public TokenException(string t)
        {
            text = t;
        }

        override public string Message
        {
            get
            {
                return "Unable to parse " + text + ".";
            }
        }
    }

    class BogusArithmeticException : ParserException
    {
        private string text;

        public BogusArithmeticException(string t)
        {
            text = t;
        }
        
        override public string Message
        {
            get
            {
                return "Sorry, without bignums we can't handle numbers like " +
                    text + ".";
            }
        }
    }

    abstract class EvaluatorException : SharpFException
    {
    }

    class UnboundSymbolException : EvaluatorException
    {
        private Symbol sym;

        public UnboundSymbolException(Symbol s)
        {
            sym = s;
        }

        override public string Message
        {
            get
            {
                return "Unbound symbol " + sym.name + ".";
            }
        }
    }

    class MissingArgumentException : EvaluatorException
    {
        private string func;
        private int numSupplied;
        private int numRequired;

        public MissingArgumentException(string f, int sup, int req)
        {
            func = f; numSupplied = sup; numRequired = req;
        }

        override public string Message
        {
            get
            {
                return func + " expects " +
                    numRequired.ToString() + " arguments, but " +
                    numSupplied.ToString() + " were supplied.";
            }
        }
    }

    class ArgumentTypeException : EvaluatorException
    {
        private string func;
        private int index;
        private Type supplied;
        private Type required;

        public ArgumentTypeException(string f, int i, Type s, Type r)
        {
            func = f; index = i; supplied = s; required = r;
        }

        override public string Message
        {
            get
            {
                return func + " expects argument " +
                    index.ToString() + " to be " +
                    required.Name + ", but " + supplied.Name +
                    " was supplied.";
            }
        }
    }

    class BadFormException : EvaluatorException
    {
        private string form;

        public BadFormException(string f)
        {
            form = f;
        }

        override public string Message
        {
            get
            {
                return "Malformed " + form + ".";
            }
        }
    }

    class InapplicableException : EvaluatorException
    {
        private Datum notFunc;

        public InapplicableException(Datum nf)
        {
            notFunc = nf;
        }

        override public string Message
        {
            get
            {
                return Shell.Format(notFunc) + " is not a function.";
            }
        }
    }
}
