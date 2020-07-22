namespace evaluator
{
    using System.Collections.Generic;

    class evaluator_builtins
    {
        public static Dictionary<string, Object.Builtin> builtins = new Dictionary<string, Object.Builtin>
        {
            {"len" , Object.builtins.GetBuiltinByName("len")},
            {"puts" , Object.builtins.GetBuiltinByName("puts")},
            {"first" , Object.builtins.GetBuiltinByName("first")},
            {"last" , Object.builtins.GetBuiltinByName("last")},
            {"rest" , Object.builtins.GetBuiltinByName("rest")},
            {"push" , Object.builtins.GetBuiltinByName("push")},
        };
    }
}
