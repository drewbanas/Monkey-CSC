namespace vm
{
    using Instructions = System.Collections.Generic.List<System.Byte>;

    struct Frame_t
    {
        public Object.Closure cl;
        public int ip;
        public int basePointer;
    }

    class Frame
    {
        public static Frame_t NewFrame(Object.Closure cl, int basePointer)
        {
            Frame_t f = new Frame_t
            {
                cl = cl,
                ip = -1,
                basePointer = basePointer,
            };

            return f;
        }

        public static Instructions Instructions(Frame_t f)
        {
            return f.cl.Fn.Instructions;
        }
    }
}
