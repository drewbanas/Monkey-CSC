namespace runFile
{
    using lexer;
    using parser;
    using compiler;
    using vm;
    using util;

    using error = System.String;

    class runFile
    {
        static string readFile(string path)
        {
            System.Text.StringBuilder buffer = null;
            if (!System.IO.File.Exists(path))
            {
                System.Console.WriteLine("Could not open file {0}.", path);
                System.Environment.Exit(74);
            }
            buffer = new System.Text.StringBuilder(System.IO.File.ReadAllText(path));
            buffer.Append('\0');
            if (buffer == null)
            {
                System.Console.WriteLine("Not enough memory to read {0}.", path);
                System.Environment.Exit(74);
            }
            return buffer.ToString();
        }

        public static void Start(string path)
        {
            // Benchmarking variables
            System.DateTime start = new System.DateTime();
            System.TimeSpan duration = new System.TimeSpan();
            Object.Object result = null;

            string input = readFile(path);
            Lexer l = Lexer.New(input);
            Parser p = Parser.New(l);

            ast.Program program = p.ParseProgram();
            if (Parser.Errors().Count != 0)
            {
                repl.repl.printParserErrors(Parser.Errors());
                System.Console.ReadKey();
                System.Environment.Exit(77);
            }

            if (flag.EngineType == flag.engineType.vm)
            {
                Compiler_t comp = Compiler.New();
                Compiler._SetCompiler(ref comp);

                error err = Compiler.Compile(program);
                if (err != null)
                {
                    System.Console.WriteLine("Woops! Compilation failed:\n {0}", err);
                    System.Console.ReadKey();
                    System.Environment.Exit(78);
                }

                VM_t machine = VM.New(Compiler.Bytecode());
                VM._SetVM(ref machine); // work around

                if (flag.EnableBenchmark)
                {
                    start = System.DateTime.Now;
                }


                err = VM.Run();
                if (err != null)
                {
                    System.Console.WriteLine("Woops! Executing bytecode failed:\n {0}", err);
                    System.Console.ReadKey();
                    System.Environment.Exit(79);
                }

                if (flag.EnableBenchmark)
                {
                    duration = System.DateTime.Now.Subtract(start);
                }

                Object.Object lastPopped = VM.LastPoppedStackElem();
                System.Console.Write(lastPopped.Inspect());
                System.Console.WriteLine();

                if (flag.EnableBenchmark)
                    result = lastPopped;
            }
            else
            {
                Object.Environment env = Object.Environment.NewEnvironment();
                
                if (flag.EnableBenchmark)
                {
                    start = System.DateTime.Now;
                }

                Object.Object evaluated = evaluator.evaluator.Eval(program, env);

                if (flag.EnableBenchmark)
                {
                    duration = System.DateTime.Now.Subtract(start);
                }

                if (evaluated != null)
                {
                    System.Console.Write(evaluated.Inspect());
                    System.Console.WriteLine();
                }

                if (flag.EnableBenchmark)
                    result = evaluated;
            }

            if (flag.EnableBenchmark)
            {
                System.Console.WriteLine
                    (
                    "\nengine={0}, result={1}, duration={2}s",
                    flag.EngineType.ToString(),
                    result.Inspect(),
                    duration.TotalSeconds.ToString()
                    );

                System.Console.ReadKey();
            }
        }
    }
}
