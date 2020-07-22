namespace Object
{
    using System.Collections.Generic;

    struct _BuiltinDefinition
    {
        public string Name;
        public Builtin Builtin;
    }

    class builtins
    {
        public static _BuiltinDefinition[] Builtins = new _BuiltinDefinition[]
        {
            new _BuiltinDefinition { Name = "len", Builtin = new Builtin { Fn = len} },
            new _BuiltinDefinition { Name = "puts", Builtin = new Builtin { Fn = puts} },
            new _BuiltinDefinition { Name = "first", Builtin = new Builtin { Fn = first} },
            new _BuiltinDefinition { Name = "last", Builtin = new Builtin { Fn = last} },
            new _BuiltinDefinition { Name = "rest", Builtin = new Builtin { Fn = rest} },
            new _BuiltinDefinition { Name = "push", Builtin = new Builtin { Fn = push} },
        };


        static Object len(List<Object> args)
        {
            if (args.Count != 1)
            {
                return newError("wrong number of arguments. got={0:D}, want=1", args.Count.ToString());
            }

            Object arg = args[0];
            if (arg is Array)
                return new Integer { Value = (long)((Array)arg).Elements.Count };
            if (arg is String)
                return new Integer { Value = (long)((String)arg).Value.Length };

            // default:
            return newError("argument to 'len' not supported, got {0}", args[0].Type());
        }

        static Object puts(List<Object> args)
        {
            foreach (Object arg in args)
            {
                System.Console.WriteLine(arg.Inspect());
            }

            return null;
        }

        static Object first(List<Object> args)
        {
            if (args.Count != 1)
            {
                return newError("wrong number of arguments. got={0:D}, want=1", args.Count.ToString());
            }

            if (args[0].Type() != _ObjType.ARRAY_OBJ)
            {
                return newError("argument to 'first' must be ARRAY. got={0}", args[0].Type());
            }

            Array arr = (Array)args[0];
            if (arr.Elements.Count > 0)
            {
                return arr.Elements[0];
            }

            return null;
        }

        static Object last(List<Object> args)
        {
            if (args.Count != 1)
            {
                return newError("wrong number of arguments. got={0:D}, want=1", args.Count.ToString());
            }

            if (args[0].Type() != _ObjType.ARRAY_OBJ)
            {
                return newError("argument to 'last' must be ARRAY. got={0}", args[0].Type());
            }

            Array arr = (Array)args[0];
            int length = arr.Elements.Count;
            if (length > 0)
            {
                return arr.Elements[length - 1];
            }

            return null;
        }


        static Object rest(List<Object> args)
        {
            if (args.Count != 1)
            {
                return newError("wrong number of arguments. got={0:D}, want=1", args.Count.ToString());
            }

            if (args[0].Type() != _ObjType.ARRAY_OBJ)
            {
                return newError("argument to 'rest' must be ARRAY. got={0}", args[0].Type());
            }

            Array arr = (Array)args[0];
            int length = arr.Elements.Count;
            if (length > 0)
            {                
                List<Object> newElements = new List<Object>(new Object[length - 1]);

                for (int i = 1; i < length; i++)
                    newElements[i - 1] = arr.Elements[i];

                return new Array { Elements = newElements };
            }

            return null;
        }

        static Object push(List<Object> args)
        {
            if (args.Count != 2)
            {
                return newError("wrong number of arguments. got={0:D}, want=2", args.Count.ToString());
            }

            if (args[0].Type() != _ObjType.ARRAY_OBJ)
            {
                return newError("argument to 'push' must be ARRAY. got={0}", args[0].Type());
            }

            Array arr = (Array)args[0];
            int length = arr.Elements.Count;

            List<Object> newElements = new List<Object>(new Object[length + 1]);
            for (int i = 0; i < length; i++)
                newElements[i] = arr.Elements[i];
            newElements[length] = args[1];

            return new Array { Elements = newElements };
        }


        static Error newError(string format, params string[] a)
        {
            return new Error { Message = string.Format(format, a) };
        }

        public static Builtin GetBuiltinByName(string name)
        {
            foreach (_BuiltinDefinition def in Builtins)
            {
                if (def.Name == name)
                {
                    return def.Builtin;
                }
            }
            return null;
        }
    }
}
