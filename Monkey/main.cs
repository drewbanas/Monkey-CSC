namespace monkey
{
    using repl;
    using runFile;
    using util;

    class main
    {
        static void Main(string[] args)
        {
            flag.Parse(args);

            if (flag.RunType == flag.runType.repl)
            {
                System.Console.WriteLine("\nHello {0}! This is the Monkey programming language!\n", System.Environment.UserName);
                System.Console.WriteLine("Feel free to type in comands\n");

                repl.Start();
            }
            else
            {
                runFile.Start(args[flag.ArgsFileIndex]);
            }
        }
    }
}
