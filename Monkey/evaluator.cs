namespace evaluator
{
    using System.Collections.Generic;

    struct evaluator
    {
        public static Object.Null NULL = new Object.Null { };
        public static Object.Boolean TRUE = new Object.Boolean { Value = true };
        public static Object.Boolean FALSE = new Object.Boolean { Value = false };

        public static Object.Object Eval(ast.Node node, Object.Environment env)
        {
            // Statements
            if (node is ast.Program)
                return evalProgram((ast.Program)node, env);

            if (node is ast.BlockStatement)
                return evalBlockStatement((ast.BlockStatement)node, env);

            if (node is ast.ExpressionStatement)
                return Eval(((ast.ExpressionStatement)node).Expression, env);

            if (node is ast.ReturnStatement)
            {
                Object.Object val = Eval(((ast.ReturnStatement)node).ReturnValue, env);
                if (isError(val))
                {
                    return val;
                }
                return new Object.ReturnValue { Value = val };
            }

            if (node is ast.LetStatement)
            {
                Object.Object val = Eval(((ast.LetStatement)node).Value, env);
                if (isError(val))
                {
                    return val;
                }
                env.Set(((ast.LetStatement)node).Name.Value, val);

                return null;
            }

            // Expressions
            if (node is ast.IntegerLiteral)
                return new Object.Integer { Value = ((ast.IntegerLiteral)node).Value };

            if (node is ast.StringLiteral)
                return new Object.String { Value = ((ast.StringLiteral)node).Value };

            if (node is ast.Boolean)
                return nativeToBooleanObject(((ast.Boolean)node).Value);

            if (node is ast.PrefixExpression)
            {
                Object.Object right = Eval(((ast.PrefixExpression)node).Right, env);
                if (isError(right))
                {
                    return right;
                }
                return evalPrefixExpression(((ast.PrefixExpression)node).Operator, right);
            }

            if (node is ast.InfixExpression)
            {
                Object.Object left = Eval(((ast.InfixExpression)node).Left, env);
                if (isError(left))
                {
                    return left;
                }

                Object.Object right = Eval(((ast.InfixExpression)node).Right, env);
                if (isError(right))
                {
                    return right;
                }

                return evalInfixExpression(((ast.InfixExpression)node).Operator, left, right);
            }


            if (node is ast.IfExpression)
                return evalIfExpression((ast.IfExpression)node, env);


            if (node is ast.Identifier)
                return evalIdentifier((ast.Identifier)node, env);

            if (node is ast.FunctionLiteral)
            {
                List<ast.Identifier> params_ = ((ast.FunctionLiteral)node).Parameters;
                ast.BlockStatement body = ((ast.FunctionLiteral)node).Body;
                return new Object.Function { Parameters = params_, Env = env, Body = body };
            }

            if (node is ast.CallExpression)
            {
                Object.Object function = Eval(((ast.CallExpression)node).Function, env);
                if (isError(function))
                {
                    return function;
                }

                List<Object.Object> args = evalExpressions(((ast.CallExpression)node).Arguments, env);
                if (args.Count == 1 && isError(args[0]))
                {
                    return args[0];
                }

                return applyFunction(function, args);
            }

            if (node is ast.ArrayLiteral)
            {
                List<Object.Object> elements = evalExpressions(((ast.ArrayLiteral)node).Elements, env);
                if (elements.Count == 1 && isError(elements[0]))
                {
                    return elements[0];
                }
                return new Object.Array { Elements = elements };
            }

            if (node is ast.IndexExpression)
            {
                Object.Object left = Eval(((ast.IndexExpression)node).Left, env);
                if (isError(left))
                {
                    return left;
                }
                Object.Object index = Eval(((ast.IndexExpression)node).Index, env);
                if (isError(index))
                {
                    return index;
                }
                return evalIndexExpression(left, index);
            }

            if (node is ast.HashLiteral)
                return evalHashLiteral((ast.HashLiteral)node, env);

            return null;
        }

        static Object.Object evalProgram(ast.Program program, Object.Environment env)
        {
            Object.Object result = null;

            foreach (ast.Statement statement in program.Statements)
            {
                result = Eval(statement, env);

                if (result is Object.ReturnValue)
                    return ((Object.ReturnValue)result).Value;

                if (result is Object.Error)
                    return result;
            }

            return result;
        }

        static Object.Object evalBlockStatement(ast.BlockStatement block, Object.Environment env)
        {
            Object.Object result = null;

            foreach (ast.Statement statement in block.Statements)
            {
                result = Eval(statement, env);

                if (result != null)
                {
                    string rt = result.Type();
                    if (rt == Object._ObjType.RETURN_VALUE_OBJ || rt == Object._ObjType.ERROR_OBJ)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        static Object.Boolean nativeToBooleanObject(bool input)
        {
            if (input)
            {
                return TRUE;
            }
            return FALSE;
        }

        static Object.Object evalPrefixExpression(string operator_, Object.Object right)
        {
            switch (operator_)
            {
                case "!":
                    return evalBangOperatorExpression(right);
                case "-":
                    return evalMinusPrefixOperatorExpression(right);
                default:
                    return newError("unknown operator {0}{1}", operator_, right.Type());
            }
        }

        static Object.Object evalInfixExpression(string operator_, Object.Object left, Object.Object right)
        {
            if (left.Type() == Object._ObjType.INTEGER_OBJ && right.Type() == Object._ObjType.INTEGER_OBJ)
                return evalIntegerInfixExpression(operator_, left, right);

            if (left.Type() == Object._ObjType.STRING_OBJ && right.Type() == Object._ObjType.STRING_OBJ)
                return evalStringInfixExpression(operator_, left, right);

            if (operator_ == "==")
                return nativeToBooleanObject(left == right);

            if (operator_ == "!=")
                return nativeToBooleanObject(left != right);

            if (left.Type() != right.Type())
                return newError("type mismatch: {0} {1} {2}", left.Type(), operator_, right.Type());

            // default:
            return newError("unknown operator: {0} {1} {2}", left.Type(), operator_, right.Type());
        }

        static Object.Object evalBangOperatorExpression(Object.Object right)
        {
            if (right.Equals(TRUE))
                return FALSE;

            if (right.Equals(FALSE))
                return TRUE;

            if (right.Equals(NULL))
                return TRUE;

            return FALSE;
        }

        static Object.Object evalMinusPrefixOperatorExpression(Object.Object right)
        {
            if (right.Type() != Object._ObjType.INTEGER_OBJ)
            {
                return newError("unknown operator: -{0}", right.Type());
            }

            long value = ((Object.Integer)right).Value;
            return new Object.Integer { Value = -value };
        }

        static Object.Object evalIntegerInfixExpression(string operator_, Object.Object left, Object.Object right)
        {
            long leftVal = ((Object.Integer)left).Value;
            long rightVal = ((Object.Integer)right).Value;

            switch (operator_)
            {
                case "+":
                    return new Object.Integer { Value = leftVal + rightVal };
                case "-":
                    return new Object.Integer { Value = leftVal - rightVal };
                case "*":
                    return new Object.Integer { Value = leftVal * rightVal };
                case "/":
                    return new Object.Integer { Value = leftVal / rightVal };
                case "<":
                    return nativeToBooleanObject(leftVal < rightVal);
                case ">":
                    return nativeToBooleanObject(leftVal > rightVal);
                case "==":
                    return nativeToBooleanObject(leftVal == rightVal);
                case "!=":
                    return nativeToBooleanObject(leftVal != rightVal);
                default:
                    return newError("unknown operator: {0} {1} {2}", left.Type(), operator_, right.Type());
            }
        }

        static Object.Object evalStringInfixExpression(string operator_, Object.Object left, Object.Object right)
        {
            if (operator_ != "+")
            {
                return newError("unknown operator: {0} {1} {2}", left.Type(), operator_, right.Type());
            }

            string leftVal = ((Object.String)left).Value;
            string rightVal = ((Object.String)right).Value;
            return new Object.String { Value = leftVal + rightVal };
        }

        static Object.Object evalIfExpression(ast.IfExpression ie, Object.Environment env)
        {
            Object.Object contion = Eval(ie.Condition, env);
            if (isError(contion))
            {
                return contion;
            }

            if (isTruthy(contion))
            {
                return Eval(ie.Consequence, env);
            }
            else if (ie.Alternative != null)
            {
                return Eval(ie.Alternative, env);
            }
            else
            {
                return NULL;
            }
        }

        static Object.Object evalIdentifier(ast.Identifier node, Object.Environment env)
        {
            Object.Object val = env.Get(node.Value);
            if (val != null)
            {
                return val;
            }

            Object.Builtin builtin;
            if (evaluator_builtins.builtins.TryGetValue(node.Value, out builtin))
            {
                return builtin;
            }

            return newError("identifier not found: " + node.Value); ;
        }

        static bool isTruthy(Object.Object obj)
        {
            if (obj.Equals(NULL))
                return false;
            if (obj.Equals(TRUE))
                return true;
            if (obj.Equals(FALSE))
                return false;

            return true;
        }

        public static Object.Error newError(string format, params string[] a)
        {
            return new Object.Error { Message = string.Format(format, a) };
        }

        static bool isError(Object.Object obj)
        {
            if (obj != null)
            {
                return obj.Type() == Object._ObjType.ERROR_OBJ;
            }
            return false;
        }

        static List<Object.Object> evalExpressions(List<ast.Expression> exps, Object.Environment env)
        {
            List<Object.Object> result = new List<Object.Object>();

            foreach (ast.Node e in exps)
            {
                Object.Object evaluated = Eval(e, env);
                if (isError(evaluated))
                {
                    return new List<Object.Object> { evaluated };
                }
                result.Add(evaluated);
            }

            return result;
        }

        static Object.Object applyFunction(Object.Object fn, List<Object.Object> args)
        {
            if (fn is Object.Function)
            {
                Object.Environment extendedEnv = extendFunctionEnv(((Object.Function)fn), args);
                Object.Object evaluated = Eval(((Object.Function)fn).Body, extendedEnv);
                return unwrapReturnValue(evaluated);
            }

            if (fn is Object.Builtin)
            {
                Object.Object result = ((Object.Builtin)fn).Fn(args);
                if(result != null)
                {
                    return result;
                }
                return NULL;
            }

            // default:
            return newError("not a function {0}", fn.Type());
        }

        static Object.Environment extendFunctionEnv(Object.Function fn, List<Object.Object> args)
        {
            Object.Environment env = Object.Environment.NewEnclosedEnvironment(fn.Env);

            for (int paramIdx = 0; paramIdx < args.Count; paramIdx++)
            {
                env.Set(fn.Parameters[paramIdx].Value, args[paramIdx]);
            }

            return env;
        }

        static Object.Object unwrapReturnValue(Object.Object obj)
        {
            if (obj is Object.ReturnValue)
            {
                return ((Object.ReturnValue)obj).Value;
            }

            return obj;
        }

        static Object.Object evalIndexExpression(Object.Object left, Object.Object index)
        {
            if (left.Type() == Object._ObjType.ARRAY_OBJ && index.Type() == Object._ObjType.INTEGER_OBJ)
                return evalArrayIndexExpression(left, index);

            if (left.Type() == Object._ObjType.HASH_OBJ)
                return evalHashIndexExpression(left, index);

            // default
            return newError("index operator not supported: {0}", left.Type());
        }

        static Object.Object evalArrayIndexExpression(Object.Object array, Object.Object index)
        {
            Object.Array arrayObject = (Object.Array)array;
            long idx = ((Object.Integer)index).Value;
            long max = (long)(arrayObject.Elements.Count - 1);

            if (idx < 0 || idx > max)
            {
                return NULL;
            }

            return arrayObject.Elements[(int)idx];
        }

        static Object.Object evalHashLiteral(ast.HashLiteral node, Object.Environment env)
        {
            Dictionary<Object.HashKey, Object.HashPair> pairs = new Dictionary<Object.HashKey, Object.HashPair>();

            foreach (KeyValuePair<ast.Expression, ast.Expression> _node_pair in node.Pairs)
            {
                Object.Object key = Eval(_node_pair.Key, env);
                if (isError(key))
                {
                    return key;
                }

                if (!(key is Object.Hashable))
                {
                    return newError("unusable as hash key: {0}", key.Type());
                }
                Object.Hashable hashKey = (Object.Hashable)key;

                Object.Object value = Eval(_node_pair.Value, env);
                if (isError(value))
                {
                    return value;
                }

                Object.HashKey hashed = hashKey.HashKey();
                pairs.Add(hashed, new Object.HashPair { Key = key, Value = value });
            }

            return new Object.Hash { Pairs = pairs };
        }

        static Object.Object evalHashIndexExpression(Object.Object hash, Object.Object index)
        {
            Object.Hash hashObject = (Object.Hash)hash;

            if (!(index is Object.Hashable))
            {
                return newError("unusable as hash key: {0}", index.Type());
            }
            Object.Hashable key = (Object.Hashable)index;

            Object.HashPair pair;
            if (!hashObject.Pairs.TryGetValue(key.HashKey(), out pair))
            {
                return NULL;
            }

            return pair.Value;
        }
    }
}
