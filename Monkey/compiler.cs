namespace compiler
{
    using System.Collections.Generic;
    using ast;
    using code;

    using Instructions = System.Collections.Generic.List<System.Byte>;
    using error = System.String;
    using Opcode = System.Byte;

    struct Compiler_t
    {
        public List<Object.Object> constants;

        public symbol_table.SymbolTable symbolTable;

        public List<CompilationScope> scopes;
        public int scopeIndex;
    }

    class Compiler
    {
        // Work arounds
        static Compiler_t c;
        public static void _SetCompiler(ref Compiler_t _c) { c = _c; }

        public static Compiler_t New()
        {
            CompilationScope mainScope = new CompilationScope
            {
                instructions = new Instructions { },
                lastInstruction = new EmittedInstruction { },
                previousInstruction = new EmittedInstruction { },
            };

            symbol_table.SymbolTable symbolTable = symbol_table.NewSymbolTable();

            for (int i = 0; i < Object.builtins.Builtins.Length; i++)
            {
                Object._BuiltinDefinition v = Object.builtins.Builtins[i];
                symbol_table.DefineBuiltin(ref symbolTable, i, v.Name);
            }

            return new Compiler_t
            {
                constants = new List<Object.Object>(),
                symbolTable = symbolTable,
                scopes = new List<CompilationScope> { mainScope },
                scopeIndex = 0,
            };
        }

        public static Compiler_t NewWithState(ref symbol_table.SymbolTable s, List<Object.Object> constants)
        {
            Compiler_t compiler = New();
            compiler.symbolTable = s;
            compiler.constants = constants;
            return compiler;
        }

        public static error Compile(ast.Node node)
        {
            if (node is ast.Program)
            {
                foreach (ast.Statement s in ((ast.Program)node).Statements)
                {
                    error err = Compile(s);
                    if (err != null)
                    {
                        return err;
                    }
                }

                return null;
            }

            if (node is ast.ExpressionStatement)
            {
                error err = Compile(((ast.ExpressionStatement)node).Expression);
                if (err != null)
                {
                    return err;
                }
                emit(code.OpPop);

                return null;
            }

            if (node is ast.InfixExpression)
            {
                ast.InfixExpression _node = (ast.InfixExpression)node;
                error err;
                if (_node.Operator == "<")
                {
                    err = Compile(_node.Right);
                    if (err != null)
                    {
                        return err;
                    }

                    err = Compile(_node.Left);
                    if (err != null)
                    {
                        return err;
                    }

                    emit(code.OpGreaterThan);
                    return null;
                }

                err = Compile(_node.Left);
                if (err != null)
                {
                    return err;
                }

                err = Compile(_node.Right);
                if (err != null)
                {
                    return err;
                }

                switch (_node.Operator)
                {
                    case "+":
                        emit(code.OpAdd);
                        break;
                    case "-":
                        emit(code.OpSub);
                        break;
                    case "*":
                        emit(code.OpMul);
                        break;
                    case "/":
                        emit(code.OpDiv);
                        break;
                    case ">":
                        emit(code.OpGreaterThan);
                        break;
                    case "==":
                        emit(code.OpEqual);
                        break;
                    case "!=":
                        emit(code.OpNotEqual);
                        break;
                    default:
                        return string.Format("unknown operator {0}", _node.Operator);
                }

                return null;
            }

            if (node is ast.IntegerLiteral)
            {
                Object.Integer integer = new Object.Integer { Value = ((ast.IntegerLiteral)node).Value };
                emit(code.OpConstant, (Opcode)addConstant(integer));

                return null;
            }

            if (node is ast.Boolean)
            {
                if (((ast.Boolean)node).Value)
                {
                    emit(code.OpTrue);
                }
                else
                {
                    emit(code.OpFalse);
                }

                return null;
            }

            if (node is ast.PrefixExpression)
            {
                ast.PrefixExpression _node = (ast.PrefixExpression)node;
                error err = Compile(_node.Right);
                if (err != null)
                {
                    return err;
                }

                switch (_node.Operator)
                {
                    case "!":
                        emit(code.OpBang);
                        break;
                    case "-":
                        emit(code.OpMinus);
                        break;
                    default:
                        return string.Format("unknown operator {0}", _node.Operator);
                }

                return null;
            }

            if (node is ast.IfExpression)
            {
                ast.IfExpression _node = (ast.IfExpression)node;
                error err = Compile(_node.Condition);
                if (err != null)
                {
                    return err;
                }

                //Emit an 'OpJumpNotTruthy' with bogus value
                int jumpNotTruthyPos = emit(code.OpJumpNotTruthy, 9999);

                err = Compile(_node.Consequence);
                if (err != null)
                {
                    return err;
                }

                if (lastInstructionIs(code.OpPop))
                {
                    removeLastPop();
                }

                // Emit an 'OpJump' with a bogus value
                int jumpPos = emit(code.OpJump, 9999);

                int afterConsequencePos = currentInstructions().Count;
                changeOperand(jumpNotTruthyPos, afterConsequencePos);

                if (_node.Alternative == null)
                {
                    emit(code.OpNull);
                }
                else
                {
                    err = Compile(_node.Alternative);
                    if (err != null)
                    {
                        return err;
                    }

                    if (lastInstructionIs(code.OpPop))
                    {
                        removeLastPop();
                    }
                }

                int afterAlternativePos = currentInstructions().Count;
                changeOperand(jumpPos, afterAlternativePos);

                return null;
            }

            if (node is ast.BlockStatement)
            {
                foreach (ast.Statement s in ((ast.BlockStatement)node).Statements)
                {
                    error err = Compile(s);
                    if (err != null)
                    {
                        return err;
                    }
                }

                return null;
            }

            if (node is ast.LetStatement)
            {
                ast.LetStatement _node = (ast.LetStatement)node;

                symbol_table.Symbol symbol = symbol_table.Define(ref c.symbolTable, _node.Name.Value);
                error err = Compile(_node.Value);
                if (err != null)
                {
                    return err;
                }

                if (symbol.Scope == symbol_table.GlobalScope)
                {
                    emit(code.OpSetGlobal, symbol.Index);
                }
                else
                {
                    emit(code.OpSetLocal, symbol.Index);
                }

                return null;
            }

            if (node is ast.Identifier)
            {
                ast.Identifier _node = (ast.Identifier)node;

                symbol_table.Symbol symbol = symbol_table.Resolve(ref c.symbolTable, _node.Value);
                if (symbol == null)
                {
                    return string.Format("undefined variable {0}", _node.Value);
                }

                loadSymbols(symbol);

                return null;
            }

            if (node is ast.StringLiteral)
            {
                ast.StringLiteral _node = (ast.StringLiteral)node;
                Object.String str = new Object.String { Value = _node.Value };
                emit(code.OpConstant, addConstant(str));

                return null;
            }

            if (node is ast.ArrayLiteral)
            {
                ast.ArrayLiteral _node = (ast.ArrayLiteral)node;
                foreach (ast.Expression el in _node.Elements)
                {
                    error err = Compile(el);
                    if (err != null)
                    {
                        return err;
                    }
                }

                emit(code.OpArray, _node.Elements.Count);

                return null;
            }

            if (node is ast.HashLiteral)
            {
                ast.HashLiteral _node = (ast.HashLiteral)node;

                // the sorting is not strictly needed and was only done to pass the test
                // since the test assumes a specific order for the keys

                foreach (KeyValuePair<Expression, Expression> k in _node.Pairs)
                {
                    error err = Compile(k.Key);
                    if (err != null)
                    {
                        return err;
                    }
                    err = Compile(_node.Pairs[k.Key]);
                    if (err != null)
                    {
                        return err;
                    }
                }

                emit(code.OpHash, _node.Pairs.Count * 2);

                return null;
            }

            if (node is ast.IndexExpression)
            {
                ast.IndexExpression _node = (ast.IndexExpression)node;
                error err = Compile(_node.Left);
                if (err != null)
                {
                    return err;
                }

                err = Compile(_node.Index);
                if (err != null)
                {
                    return err;
                }

                emit(code.OpIndex);

                return null;
            }

            if (node is ast.FunctionLiteral)
            {
                ast.FunctionLiteral _node = (ast.FunctionLiteral)node;

                enterScope();

                if (_node.Name != null && _node.Name != "")
                {
                    symbol_table.DefineFunctionName(ref c.symbolTable, _node.Name);
                }

                foreach (ast.Identifier p in _node.Parameters)
                {
                    symbol_table.Define(ref c.symbolTable, p.Value);
                }

                error err = Compile(_node.Body);
                if (err != null)
                {
                    return err;
                }

                if (lastInstructionIs(code.OpPop))
                {
                    replaceLastPopWithReturn();
                }
                if (!lastInstructionIs(code.OpReturnValue))
                {
                    emit(code.OpReturn);
                }

                List<symbol_table.Symbol> freeSymbols = c.symbolTable.FreeSymbols;
                int numLocals = c.symbolTable.numDefinitions;
                Instructions instructions = leaveScope();

                foreach (symbol_table.Symbol s in freeSymbols)
                {
                    loadSymbols(s);
                }

                Object.CompiledFunction compiledFn = new Object.CompiledFunction
                {
                    Instructions = instructions,
                    NumLocals = numLocals,
                    NumParameters = _node.Parameters.Count,
                };

                int fnIndex = addConstant(compiledFn);
                emit(code.OpClosure, fnIndex, freeSymbols.Count);

                return null;
            }

            if (node is ast.ReturnStatement)
            {
                ast.ReturnStatement _node = (ast.ReturnStatement)node;
                error err = Compile(_node.ReturnValue);
                if (err != null)
                {
                    return err;
                }

                emit(code.OpReturnValue);

                return null;
            }

            if (node is ast.CallExpression)
            {
                ast.CallExpression _node = (ast.CallExpression)node;

                error err = Compile(_node.Function);
                if (err != null)
                {
                    return err;
                }

                foreach (Expression a in _node.Arguments)
                {
                    err = Compile(a);
                    if (err != null)
                    {
                        return err;
                    }
                }

                emit(code.OpCall, _node.Arguments.Count);
                return null;
            }

            return null;
        }

        public static Bytecode Bytecode()
        {
            return new Bytecode
            {
                Instructions = currentInstructions(),
                Constants = c.constants
            };
        }

        static int addConstant(Object.Object obj)
        {
            c.constants.Add(obj);
            return c.constants.Count - 1;
        }

        static int emit(Opcode op, params int[] operands)
        {
            Instructions ins = code.Make(op, operands);
            int pos = addInstruction(ins);

            setLastInstruction(op, pos);

            return pos;
        }

        static int addInstruction(List<byte> ins)
        {
            int posNewInstruction = currentInstructions().Count;
            c.scopes[c.scopeIndex].instructions.AddRange(ins);

            return posNewInstruction;
        }

        static void setLastInstruction(Opcode op, int pos)
        {
            EmittedInstruction previous = c.scopes[c.scopeIndex].lastInstruction;
            EmittedInstruction last = new EmittedInstruction { Opcode = op, Position = pos };

            CompilationScope _scope = c.scopes[c.scopeIndex];
            _scope.previousInstruction = previous;
            _scope.lastInstruction = last;
            c.scopes[c.scopeIndex] = _scope;
        }

        static bool lastInstructionIs(Opcode op)
        {
            if (currentInstructions().Count == 0)
            {
                return false;
            }
            return c.scopes[c.scopeIndex].lastInstruction.Opcode == op;
        }

        static void removeLastPop()
        {
            EmittedInstruction last = c.scopes[c.scopeIndex].lastInstruction;
            EmittedInstruction previous = c.scopes[c.scopeIndex].previousInstruction;

            List<byte> old = currentInstructions();
            List<byte> new_ = new List<byte>(last.Position + 1);
            for (int i = 0; i < last.Position; i++)
                new_.Add(old[i]);

            CompilationScope _scope = c.scopes[c.scopeIndex];
            _scope.instructions = new_;
            _scope.lastInstruction = previous;
            c.scopes[c.scopeIndex] = _scope;
        }

        static void replaceInstruction(int pos, List<byte> newInstruction)
        {
            List<byte> ins = currentInstructions();

            for (int i = 0; i < newInstruction.Count; i++)
            {
                ins[pos + i] = newInstruction[i];
            }

            CompilationScope _scope = c.scopes[c.scopeIndex];
            _scope.instructions = ins;
            c.scopes[c.scopeIndex] = _scope;
        }

        static void changeOperand(int opPos, int operand)
        {
            Opcode op = (Opcode)currentInstructions()[opPos];
            Instructions newInstruction = code.Make(op, operand);

            replaceInstruction(opPos, newInstruction);
        }

        static Instructions currentInstructions()
        {
            return c.scopes[c.scopeIndex].instructions;
        }

        static void enterScope()
        {
            CompilationScope scope = new CompilationScope
            {
                instructions = new Instructions { },
                lastInstruction = new EmittedInstruction { },
                previousInstruction = new EmittedInstruction { },
            };
            c.scopes.Add(scope);
            c.scopeIndex++;

            c.symbolTable = symbol_table.NewEnclosedSymbolTable(c.symbolTable);
        }

        static Instructions leaveScope()
        {
            Instructions instructions = currentInstructions();

            c.scopes.RemoveAt(c.scopes.Count - 1);
            c.scopeIndex--;

            c.symbolTable = c.symbolTable.Outer;

            return instructions;
        }

        static void replaceLastPopWithReturn()
        {
            int lastPos = c.scopes[c.scopeIndex].lastInstruction.Position;
            replaceInstruction(lastPos, code.Make(code.OpReturnValue));

            CompilationScope _scope = c.scopes[c.scopeIndex];
            _scope.lastInstruction.Opcode = code.OpReturnValue;
            c.scopes[c.scopeIndex] = _scope;
        }

        static void loadSymbols(symbol_table.Symbol s)
        {
            switch (s.Scope)
            {
                case symbol_table.GlobalScope:
                    emit(code.OpGetGlobal, s.Index);
                    break;
                case symbol_table.LocalScope:
                    emit(code.OpGetLocal, s.Index);
                    break;
                case symbol_table.BuiltinScope:
                    emit(code.OpGetBuiltin, s.Index);
                    break;
                case symbol_table.FreeScope:
                    emit(code.OpGetFree, s.Index);
                    break;
                case symbol_table.FunctionScope:
                    emit(code.OpCurrentClosure);
                    break;
            }
        }
    }

    struct Bytecode
    {
        public Instructions Instructions;
        public List<Object.Object> Constants;
    }

    struct EmittedInstruction
    {
        public Opcode Opcode;
        public int Position;
    }

    struct CompilationScope
    {
        public Instructions instructions;
        public EmittedInstruction lastInstruction;
        public EmittedInstruction previousInstruction;
    }
}
