namespace Object
{
    using System.Collections.Generic;

    using ObjectType = System.String;
    using Instructions = System.Collections.Generic.List<System.Byte>;

    delegate Object BuiltinFunction(List<Object> args);

    struct _ObjType
    {
        public const ObjectType NULL_OBJ = "NULL";
        public const ObjectType ERROR_OBJ = "ERROR";

        public const ObjectType INTEGER_OBJ = "INTEGER";
        public const ObjectType BOOLEAN_OBJ = "BOOLEAN";
        public const ObjectType STRING_OBJ = "STRING";

        public const ObjectType RETURN_VALUE_OBJ = "RETURN_VALUE";

        public const ObjectType FUNCTION_OBJ = "FUNCTION";
        public const ObjectType BUILTIN_OBJ = "BUILTIN";

        public const ObjectType ARRAY_OBJ = "ARRAY";
        public const ObjectType HASH_OBJ = "HASH";

        public const ObjectType COMPILED_FUNCTION_OBJ = "COMPILED_FUNCTION_OBJ";

        public const ObjectType CLOSURE_OBJ = "CLOSURE";
    }

    struct HashKey
    {
        public ObjectType Type;
        public long Value;
    }

    interface Hashable
    {
        HashKey HashKey();
    }

    interface Object
    {
        ObjectType Type();
        string Inspect();
    }

    struct Integer : Object, Hashable
    {
        public long Value;

        public ObjectType Type() { return _ObjType.INTEGER_OBJ; }
        public string Inspect() { return Value.ToString(); }
        public HashKey HashKey()
        {
            return new HashKey { Type = this.Type(), Value = (long)this.Value };
        }
    }

    struct Boolean : Object, Hashable
    {
        public bool Value;

        public ObjectType Type() { return _ObjType.BOOLEAN_OBJ; }
        public string Inspect() { return Value.ToString(); }
        public HashKey HashKey()
        {
            long value;

            if (this.Value)
            {
                value = 1;
            }
            else
            {
                value = 0;
            }
            return new HashKey { Type = this.Type(), Value = value };
        }
    }

    struct Null : Object
    {
        public ObjectType Type() { return _ObjType.NULL_OBJ; }
        public string Inspect() { return "null"; }
    }

    struct ReturnValue : Object
    {
        public Object Value;

        public ObjectType Type() { return _ObjType.RETURN_VALUE_OBJ; }
        public string Inspect() { return this.Value.Inspect(); }
    }

    struct Error : Object
    {
        public string Message;

        public ObjectType Type() { return _ObjType.ERROR_OBJ; }
        public string Inspect() { return "ERROR: " + this.Message; }
    }

    struct Function : Object
    {
        public List<ast.Identifier> Parameters;
        public ast.BlockStatement Body;
        public Environment Env;

        public ObjectType Type() { return _ObjType.FUNCTION_OBJ; }
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> params_ = new List<string>();
            foreach (ast.Identifier p in this.Parameters)
            {
                params_.Add(p.String());
            }

            out_.Append("fn");
            out_.Append("(");
            out_.Append(string.Join(", ", params_));
            out_.Append(") {\n");
            out_.Append(this.Body.String());
            out_.Append("\n}");

            return out_.ToString();
        }
    }

    struct String : Object, Hashable
    {
        public string Value;

        public ObjectType Type() { return _ObjType.STRING_OBJ; }
        public string Inspect() { return this.Value; }
        public HashKey HashKey()
        {
            long h = util.hash.hashString(this.Value);
            return new HashKey { Type = this.Type(), Value = h };
        }
    }

    class Builtin : Object
    {
        public BuiltinFunction Fn;

        public ObjectType Type() { return _ObjType.BUILTIN_OBJ; }
        public string Inspect() { return "builtin function"; }
    }

    struct Array : Object
    {
        public List<Object> Elements;

        public ObjectType Type() { return _ObjType.ARRAY_OBJ; }
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> elements = new List<string>();
            foreach (Object e in this.Elements)
            {
                elements.Add(e.Inspect());
            }

            out_.Append("[");
            out_.Append(string.Join(", ", elements));
            out_.Append("]");

            return out_.ToString();
        }
    }

    struct HashPair
    {
        public Object Key;
        public Object Value;
    }

    struct Hash : Object
    {
        public Dictionary<HashKey, HashPair> Pairs;

        public ObjectType Type() { return _ObjType.HASH_OBJ; }
        public string Inspect()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> pairs = new List<string>();
            foreach (KeyValuePair<HashKey, HashPair> pair in this.Pairs)
            {
                pairs.Add(string.Format("{0}: {1}", pair.Value.Key.Inspect(), pair.Value.Value.Inspect()));
            }

            out_.Append("{");
            out_.Append(string.Join(", ", pairs));
            out_.Append("}");

            return out_.ToString();
        }
    }

    struct CompiledFunction : Object
    {
        public Instructions Instructions;
        public int NumLocals;
        public int NumParameters;

        public ObjectType Type() { return _ObjType.COMPILED_FUNCTION_OBJ; }
        public string Inspect()
        {
            return string.Format("Compiled function [{0:D}]", this.GetHashCode());
        }
    }

    struct Closure: Object
    {
        public CompiledFunction Fn;
        public List<Object> Free;

        public ObjectType Type() { return _ObjType.CLOSURE_OBJ; }
        public string Inspect()
        {
            return string.Format("Closure [{0:D}]", this.GetHashCode());
        }
    }
}
