namespace Object
{
    using System.Collections.Generic;

    class Environment
    {
        Dictionary<string, Object> store;
        Environment outer;

        public static Environment NewEnclosedEnvironment(Environment outer)
        {
            Environment env = NewEnvironment();
            env.outer = outer;
            return env;
        }

        public static Environment NewEnvironment()
        {
            Dictionary<string, Object> s = new Dictionary<string, Object>();
            return new Environment { store = s };
        }

        public Object Get(string name)
        {
            Object obj = null;
            if (!this.store.TryGetValue(name, out obj) && this.outer != null)
            {
                Object _outer_obj_retrieved = this.outer.Get(name);
                if (_outer_obj_retrieved != null)
                    obj = _outer_obj_retrieved;
            }
            return obj;
        }

        public Object Set(string name, Object val)
        {
            if (this.store.ContainsKey(name))
                this.store[name] = val;
            else
                this.store.Add(name, val);

            return val;
        }
    }
}
