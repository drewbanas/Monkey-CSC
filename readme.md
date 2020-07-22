Monkey CSC
---

A C\# port of the Monkey bytecode virtual machine "compiler" from Thorsten Ball’s "*[Writing A Compiler In Go](https://compilerbook.com/)*". Other than the ability to use different "engines", the external functionality is indistinguishable from the [interpreter](https://github.com/drewbanas/Monkey-CSI). The compilation is the **translation** of the source code into bytecode representation executed by the VM. No executable files are produced as one might expect from popular compilers.

## Usage
- Without command line arguments, the program goes into REPL mode.
- Following Chapter 10, the engine may be switched between the tree-walking evaluator or the virtual machine by using the following command line arguments:
-- "-engine=vm" , to use the virtual machine (default)
-- "-engine=eval", to use the tree-walking interpreter.
- If a script file name is supplied as one of the command line arguments and an engine is selected as above, then the execution time (in seconds) will be printed when the script finishes.
- For creating scripts, please refer to the [Monkey Programming language](https://monkeylang.org/).
- Visual Studio 2015 or later is needde to compile the C\# code.

## Differences

Most differences are due to porting Go to C\# are listed in the [interpreter implementation](https://github.com/drewbanas/Monkey-CSI). Go for example has tuples, slices and more flexible switch constructs. Go also doesn't require casting to access fields of derived classes or interfaces when the identifier's defined type is the base type. Altough subtle in terms of syntax, different behaviours for reference and lvalue types, requires some workarounds.

-  Hash table are not sorted. This is done in the book for the tests to pass as the checking assumes a particular order of the hash table entries.
-  When returning CompiledFunctions and Closures, Inspect() uses hashes instead of pointers to avoid resorting to "unsafe" C\# code.
- Expressions such as "currentFrame().ip++" do not work, hence the content of currentFrame() is "inlined" to modify it's supposed returned variable.
- Similarly, the fields of contents of the compiler's scopes array could not be modified directly, so it first had to be copied, modified, then re-assigned.
- Subfolders are not used, hence evaluator_builtins.cs is named as such to distinguish from Object namespace's builtins.cs.
- The "vm" and "c" identifiers for the VM and Compiler are respectively implemented as static fields of instances VM_t and Compiler_t, while the methods are on separate classes. Approximating Go's syntax for method owners would mean adding them as function arguments which would otherwise make the code messy. Their instances are then "manually" assigned to the class containing the methods.