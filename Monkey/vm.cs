namespace vm
{
    using System.Collections.Generic;
    using code;

    using error = System.String;
    using Opcode = System.Byte;
    using Instructions = System.Collections.Generic.List<System.Byte>;

    class VM_t
    {
        public List<Object.Object> constants;
        public List<Object.Object> globals;
        public List<Object.Object> stack;

        public int sp; // Always points to the next value. Top of stack is stack[sp-1].

        public Frame_t[] frames;
        public int frameIndex;
    }

    class VM
    {
        public const int StackSize = 2048;
        public const int GlobalSize = 65536;
        public const int MaxFrames = 1024;

        public static Object.Boolean True = new Object.Boolean { Value = true };
        public static Object.Boolean False = new Object.Boolean { Value = false };
        public static Object.Null Null = new Object.Null { };

        // Work arounds
        static VM_t vm;
        public static void _SetVM(ref VM_t _vm) { vm = _vm; }

        public static VM_t New(compiler.Bytecode bytecode)
        {
            Object.CompiledFunction mainFn = new Object.CompiledFunction { Instructions = bytecode.Instructions };
            Object.Closure mainClosure = new Object.Closure { Fn = mainFn };
            Frame_t mainFrame = Frame.NewFrame(mainClosure, 0);
            Frame_t[] frames = new Frame_t[MaxFrames];
            frames[0] = mainFrame;

            return new VM_t
            {
                constants = bytecode.Constants,

                stack = new List<Object.Object>(new Object.Object[StackSize]),
                sp = 0,

                globals = new List<Object.Object>(new Object.Object[GlobalSize]),

                frames = frames,
                frameIndex = 1,
            };
        }

        public static VM_t NewWithGlobalStore(compiler.Bytecode bytecode, ref List<Object.Object> s)
        {
            VM_t vm = New(bytecode);
            vm.globals = s;

            return vm;
        }

        public static Object.Object LastPoppedStackElem()
        {
            return vm.stack[vm.sp];
        }


        public static error Run()
        {
            int ip;
            Instructions ins;
            Opcode op;

            while (currentFrame().ip < Frame.Instructions(currentFrame()).Count - 1)
            {
                vm.frames[vm.frameIndex - 1].ip++;

                ip = currentFrame().ip;
                ins = Frame.Instructions(currentFrame());
                op = (Opcode)ins[ip];

                switch (op)
                {
                    case code.OpConstant:
                        {
                            int constIndex = code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            error err = push(vm.constants[constIndex]);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpPop:
                        pop();
                        break;

                    case code.OpAdd:
                    case code.OpSub:
                    case code.OpMul:
                    case code.OpDiv:
                        {
                            error err = executeBinaryOperation(op);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpTrue:
                        {
                            error err = push(True);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpFalse:
                        {
                            error err = push(False);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpEqual:
                    case code.OpNotEqual:
                    case code.OpGreaterThan:
                        {
                            error err = executeComparison(op);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpBang:
                        {
                            error err = executeBangOperator();
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpMinus:
                        {
                            error err = executeMinusOperator();
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpJump:
                        {
                            int pos = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip = pos - 1;
                        }
                        break;

                    case code.OpJumpNotTruthy:
                        {
                            int pos = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            Object.Object condition = pop();
                            if (!isTruthy(condition))
                            {
                                vm.frames[vm.frameIndex - 1].ip = pos - 1;
                            }
                        }
                        break;

                    case code.OpNull:
                        {
                            error err = push(Null);
                            if (err != null) 
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpSetGlobal:
                        {
                            int globalIndex = code.ReadUint16(ins, ip + 1);

                            vm.frames[vm.frameIndex - 1].ip += 2;

                            vm.globals[globalIndex] = pop();
                        }
                        break;

                    case code.OpGetGlobal:
                        {
                            int globalIndex = code.ReadUint16(ins, ip + 1);

                            vm.frames[vm.frameIndex - 1].ip += 2;

                            error err = push(vm.globals[globalIndex]);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpArray:
                        {
                            int numElements = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            Object.Object array = buildArray(vm.sp - numElements, vm.sp);
                            vm.sp = vm.sp - numElements;

                            error err = push(array);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpHash:
                        {
                            int numElements = (int)code.ReadUint16(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 2;

                            error err;
                            Object.Object hash = buildHash(vm.sp - numElements, vm.sp, out err);
                            if (err != null)
                            {
                                return err;
                            }
                            vm.sp = vm.sp - numElements;

                            err = push(hash);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpIndex:
                        {
                            Object.Object index = pop();
                            Object.Object left = pop();

                            error err = executeIndexExpression(left, index);
                            if (err != null)
                            {
                                return err;
                            }

                        }
                        break;

                    case code.OpCall:
                        {
                            int numArgs = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            error err = executeCall(numArgs);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpReturnValue:
                        {
                            Object.Object returnValue = pop();

                            Frame_t frame = popFrame();
                            vm.sp = frame.basePointer - 1;

                            error err = push(returnValue);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpReturn:
                        {
                            Frame_t frame = popFrame();
                            vm.sp = frame.basePointer - 1;

                            error err = push(Null);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpSetLocal:
                        {
                            int localIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Frame_t frame = currentFrame();

                            vm.stack[frame.basePointer + localIndex] = pop();
                        }

                        break;

                    case code.OpGetLocal:
                        {
                            int localIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Frame_t frame = currentFrame();

                            error err = push(vm.stack[frame.basePointer + localIndex]);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpGetBuiltin:
                        {
                            int builtinIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Object._BuiltinDefinition definition = Object.builtins.Builtins[builtinIndex];

                            error err = push(definition.Builtin);
                            if (err != null)
                            {
                                return err;
                            }

                        }
                        break;

                    case code.OpClosure:
                        {
                            int constIndex = (int)code.ReadUint16(ins, ip + 1);
                            int numFree = (int)code.ReadUint8(ins, ip + 3);
                            vm.frames[vm.frameIndex - 1].ip += 3;

                            error err = pushClosure(constIndex, numFree);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpGetFree:
                        {
                            int freeIndex = (int)code.ReadUint8(ins, ip + 1);
                            vm.frames[vm.frameIndex - 1].ip += 1;

                            Object.Closure curentClosure = currentFrame().cl;
                            error err = push(curentClosure.Free[freeIndex]);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;

                    case code.OpCurrentClosure:
                        {
                            Object.Closure currentClosure = currentFrame().cl;
                            error err = push(currentClosure);
                            if (err != null)
                            {
                                return err;
                            }
                        }
                        break;
                }
            }

            return null;
        }

        static error push(Object.Object o)
        {
            if (vm.sp >= StackSize)
            {
                return "stack overflow";
            }

            vm.stack[vm.sp] = o;
            vm.sp++;

            return null;
        }

        static Object.Object pop()
        {
            Object.Object o = vm.stack[vm.sp - 1];
            vm.sp--;
            return o;
        }


        static error executeBinaryOperation(Opcode op)
        {
            Object.Object right = pop();
            Object.Object left = pop();

            string leftType = left.Type();
            string rightType = right.Type();

            if (leftType == Object._ObjType.INTEGER_OBJ && rightType == Object._ObjType.INTEGER_OBJ)
            {
                return executeBinaryIntegerOperation(op, left, right);
            }

            if (leftType == Object._ObjType.STRING_OBJ && rightType == Object._ObjType.STRING_OBJ)
            {
                return executeBinaryStringOperation(op, left, right);
            }

            // default:
            return string.Format("unsupported typesfor binary opreation: {0} {1}", leftType, rightType);
        }

        static error executeBinaryIntegerOperation(Opcode op, Object.Object left, Object.Object right)
        {
            long leftValue = ((Object.Integer)left).Value;
            long rightValue = ((Object.Integer)right).Value;

            long result;

            switch (op)
            {
                case code.OpAdd:
                    result = leftValue + rightValue;
                    break;
                case code.OpSub:
                    result = leftValue - rightValue;
                    break;
                case code.OpMul:
                    result = leftValue * rightValue;
                    break;
                case code.OpDiv:
                    result = leftValue / rightValue;
                    break;
                default:
                    return string.Format("unknown integer operator: {0:D}", op);
            }


            return push(new Object.Integer { Value = result });
        }

        static error executeComparison(Opcode op)
        {
            Object.Object right = pop();
            Object.Object left = pop();

            if (left.Type() == Object._ObjType.INTEGER_OBJ && right.Type() == Object._ObjType.INTEGER_OBJ)
            {
                return executeIntegerComparison(op, left, right);
            }

            switch (op)
            {
                case code.OpEqual:
                    return push(nativeToBooleanObject(right == left));
                case code.OpNotEqual:
                    return push(nativeToBooleanObject(right != left));
                default:
                    return string.Format("unsupported typesfor binary opreation: {0:D} ({1} {1})", op, left.Type(), right.Type());
            }
        }

        static error executeIntegerComparison(Opcode op, Object.Object left, Object.Object right)
        {
            long leftValue = ((Object.Integer)left).Value;
            long rightValue = ((Object.Integer)right).Value;

            switch (op)
            {
                case code.OpEqual:
                    return push(nativeToBooleanObject(rightValue == leftValue));
                case code.OpNotEqual:
                    return push(nativeToBooleanObject(rightValue != leftValue));
                case code.OpGreaterThan:
                    return push(nativeToBooleanObject(leftValue > rightValue));
                default:
                    return string.Format("unknown operator: {0:D}", op);
            }
        }


        static error executeBangOperator()
        {
            Object.Object operand = pop();

            if (operand.Equals(True))
                return push(False);
            if (operand.Equals(False))
                return push(True);
            if (operand.Equals(Null))
                return push(True);

            // default:
            return push(False);
        }

        static error executeMinusOperator()
        {
            Object.Object operand = pop();

            if (operand.Type() != Object._ObjType.INTEGER_OBJ)
            {
                return string.Format("unsupported type for negation: {0}", operand.Type());
            }

            long value = ((Object.Integer)operand).Value;
            return push(new Object.Integer { Value = -value });
        }

        static error executeBinaryStringOperation(Opcode op, Object.Object left, Object.Object right)
        {
            if (op != code.OpAdd)
            {
                return string.Format("unknown integer operator: {0:D}", op);
            }

            string leftValue = ((Object.String)left).Value;
            string rightValue = ((Object.String)right).Value;

            return push(new Object.String { Value = leftValue + rightValue });
        }

        static Object.Object buildArray(int startIndex, int endIndex)
        {
            List<Object.Object> elements = new List<Object.Object>(new Object.Object[endIndex - startIndex]);

            for (int i = startIndex; i < endIndex; i++)
            {
                elements[i - startIndex] = vm.stack[i];
            }

            return new Object.Array { Elements = elements };
        }

        static Object.Object buildHash(int startIndex, int endIndex, out error _err)
        {
            Dictionary<Object.HashKey, Object.HashPair> hashedPairs = new Dictionary<Object.HashKey, Object.HashPair>();

            for (int i = startIndex; i < endIndex; i+= 2)
            {
                Object.Object key = vm.stack[i];
                Object.Object value = vm.stack[i + 1];

                Object.HashPair pair = new Object.HashPair { Key = key, Value = value };

                if (!(key is Object.Hashable))
                {
                    _err = string.Format("unusable as hash key: {0}", key.Type());
                    return null;
                }
                Object.Hashable hashKey = (Object.Hashable)key;

                hashedPairs.Add(hashKey.HashKey(), pair);
            }

            _err = null;

            return new Object.Hash { Pairs = hashedPairs };
        }

        static error executeIndexExpression(Object.Object left, Object.Object index)
        {
            if (left.Type() == Object._ObjType.ARRAY_OBJ && index.Type() == Object._ObjType.INTEGER_OBJ)
                return executeArrayIndex(left, index);

            if (left.Type() == Object._ObjType.HASH_OBJ)
                return executeHashIndex(left, index);

            // default:
            return string.Format("index operator not supported {0}", left.Type());
        }

        static error executeArrayIndex(Object.Object array, Object.Object index)
        {
            Object.Array arrayOject = (Object.Array)array;
            long i = ((Object.Integer)index).Value;
            long max = (long)(arrayOject.Elements.Count - 1);

            if (i < 0 || i > max)
            {
                return push(Null);
            }

            return push(arrayOject.Elements[(int)i]);
        }

        static error executeHashIndex(Object.Object hash, Object.Object index)
        {
            Object.Hash hashObject = (Object.Hash)hash;

            if (!(index is Object.Hashable))
            {
                return string.Format("unusable as hash key: {0}", index.Type());
            }
            Object.Hashable key = (Object.Hashable)index;

            Object.HashPair pair;
            if (!hashObject.Pairs.TryGetValue(key.HashKey(), out pair))
            {
                return push(Null);
            }

            return push(pair.Value);
        }

        static Frame_t currentFrame()
        {
            return vm.frames[vm.frameIndex - 1];
        }

        static void pushFrame(Frame_t f)
        {
            vm.frames[vm.frameIndex] = f;
            vm.frameIndex++;
        }

        static Frame_t popFrame()
        {
            vm.frameIndex--;
            return vm.frames[vm.frameIndex];
        }

        static error executeCall(int numArgs)
        {
            Object.Object callee = vm.stack[vm.sp - 1 - numArgs];

            if (callee is Object.Closure)
                return callClosure((Object.Closure)callee, numArgs);
            if (callee is Object.Builtin)
                return callBuiltin((Object.Builtin)callee, numArgs);

            // default:
            return "calling non-function and non-built-in";
        }

        static error callClosure(Object.Closure cl, int numArgs)
        {
            if (numArgs != cl.Fn.NumParameters)
            {
                return string.Format("wrong number of arguments: want = {0:D}, got = {1:D}", cl.Fn.NumParameters, numArgs);
            }

            Frame_t frame = Frame.NewFrame(cl, vm.sp - numArgs);
            pushFrame(frame);

            vm.sp = frame.basePointer + cl.Fn.NumLocals;

            return null;
        }

        static error callBuiltin(Object.Builtin builtin, int numArgs)
        {
            List<Object.Object> args = vm.stack.GetRange(vm.sp - numArgs, numArgs);

            Object.Object result = builtin.Fn(args);
            vm.sp = vm.sp - numArgs - 1;

            if (result != null)
            {
                push(result);
            }
            else
            {
                push(Null);
            }

            return null;
        }

        static error pushClosure(int constIndex, int numFree)
        {
            Object.Object constant = vm.constants[constIndex];
            if (!(constant is Object.CompiledFunction))
            {
                return string.Format("not a function {0}", constant.ToString()); 
            }
            Object.CompiledFunction function = (Object.CompiledFunction)constant;

            List<Object.Object> free = new List<Object.Object>(new Object.Object[numFree]);
            for (int i = 0; i < numFree; i++)
            {
                free[i] = vm.stack[vm.sp - numFree - i];
            }
            vm.sp = vm.sp - numFree;

            Object.Closure closure = new Object.Closure { Fn = function, Free = free };
            return push(closure);
        }

        static Object.Boolean nativeToBooleanObject(bool input)
        {
            if (input)
            {
                return True;
            }
            return False;
        }

        static bool isTruthy(Object.Object obj)
        {
            if (obj is Object.Boolean)
                return ((Object.Boolean)obj).Value;

            if (obj is Object.Null)
                return false;

            //default:
            return true;
        }

    }
}
