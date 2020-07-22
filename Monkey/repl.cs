namespace repl
{
    using System.Collections.Generic;
    using lexer;
    using parser;
    using Object;
    using compiler;
    using vm;
    using util;

    using error = System.String;

    class repl
    {

        const string PROMPT = ">> ";

        public static void Start()
        {
            if(flag.EngineType == flag.engineType.eval)
            {
                _evalRepl();
                return; // unreachable
            }

            List<Object> constants = new List<Object>();
            List<Object> globals = new List<Object>(new Object[VM.GlobalSize]);

            symbol_table.SymbolTable symbolTable = symbol_table.NewSymbolTable();
            for (int i = 0; i < builtins.Builtins.Length; i++)
            {
                _BuiltinDefinition v = builtins.Builtins[i];
                symbol_table.DefineBuiltin(ref symbolTable, i, v.Name);
            }

            for (;;)
            {
                System.Console.Write(PROMPT);
                string line = System.Console.ReadLine();

                Lexer l = Lexer.New(line);
                Parser p = Parser.New(l);

                ast.Program program = p.ParseProgram();
                if (Parser.Errors().Count != 0)
                {
                    printParserErrors(Parser.Errors());
                    continue;
                }

                Compiler_t comp = Compiler.NewWithState(ref symbolTable, constants);
                Compiler._SetCompiler(ref comp); // work around

                error err = Compiler.Compile(program);
                if (err != null)
                {
                    System.Console.WriteLine("Woops! Compilation failed:\n {0}", err);
                    continue;
                }

                Bytecode code = Compiler.Bytecode();
                constants = code.Constants;

                VM_t machine = VM.NewWithGlobalStore(code, ref globals);
                VM._SetVM(ref machine); // work around

                err = VM.Run();
                if (err != null)
                {
                    System.Console.WriteLine("Woops! Executing bytecode failed:\n {0}", err);
                    continue;
                }

                Object lastPopped = VM.LastPoppedStackElem();
                System.Console.Write(lastPopped.Inspect());
                System.Console.WriteLine();

            }
        }

        // https://tomeko.net/online_tools/cpp_text_escape.php?lang=en
        const string MONKEY_FACE = "            __,__\n   .--.  .-\"     \"-.  .--.\n  / .. \\/  .-. .-.  \\/ .. \\\n | |  '|  /   Y   \\  |'  | |\n | \\   \\  \\ 0 | 0 /  /   / |\n  \\ '- ,\\.-\"\"\"\"\"\"\"-./, -' /\n   ''-' /_   ^ ^   _\\ '-''\n       |  \\._   _./  |\n       \\   \\ '~' /   /\n        '._ '-=-' _.'\n           '-----'";

        public static void printParserErrors(List<string> errors)
        {
            System.Console.WriteLine(MONKEY_FACE);
            System.Console.WriteLine("Woops! We ran into some monkey business here!");
            System.Console.WriteLine(" parse errors:");
            foreach (string msg in errors)
            {
                System.Console.WriteLine("\t" + msg);
            }
        }

        static void _evalRepl()
        {
            Environment env = Environment.NewEnvironment();

            for (;;)
            {
                System.Console.Write(PROMPT);
                string line = System.Console.ReadLine();

                Lexer l = Lexer.New(line);
                Parser p = Parser.New(l);

                ast.Program program = p.ParseProgram();
                if (Parser.Errors().Count != 0)
                {
                    printParserErrors(Parser.Errors());
                    continue;
                }

                Object evaluated = evaluator.evaluator.Eval(program, env);
                if (evaluated != null)
                {
                    System.Console.Write(evaluated.Inspect());
                    System.Console.WriteLine();
                }
            }
        }
    }
}
