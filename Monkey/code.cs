namespace code
{
    using System.Collections.Generic;

    using Instructions = System.Collections.Generic.List<System.Byte>;
    using Opcode = System.Byte;
    using error = System.String;
    
    class code
    {
        public static string String(Instructions ins)
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            int i = 0;
            while (i < ins.Count)
            {
                string err;
                Definition def = Lookup(ins[i], out err);
                if (err != null)
                {
                    out_.Append(string.Format("ERROR: {0}\n", err));
                    continue;
                }

                int read;
                int[] operands = ReadOperands(def, ins, i + 1, out read);

                out_.Append(string.Format("{0:D4} {1}", i, fmtInstruction(ins, def, operands)));

                i += 1 + read;
            }

            return out_.ToString();
        }


        static string fmtInstruction(Instructions ins, Definition def, int[] operands)
        {
            int operandCount = def.OperandWidths.Length;

            if (operands.Length != operandCount)
            {
                return string.Format("ERROR: operand len {0:D} does not match defined {1}\n", operands.Length, operandCount);
            }

            switch (operandCount)
            {
                case 0:
                    return def.Name;
                case 1:
                    return string.Format("{0} {1:D}", def.Name, operands[0]);
                case 2:
                    return string.Format("{0} {1:D} {2:D}", def.Name, operands[0], operands[1]);
            }

            return string.Format("ERROR: unhandled operandCount for {0}\n", def.Name);
        }

        public const Opcode OpConstant = 0;

        public const Opcode OpAdd = 1;

        public const Opcode OpPop = 2;

        public const Opcode OpSub = 3;
        public const Opcode OpMul = 4;
        public const Opcode OpDiv = 5;

        public const Opcode OpTrue = 6;
        public const Opcode OpFalse = 7;

        public const Opcode OpEqual = 8;
        public const Opcode OpNotEqual = 9;
        public const Opcode OpGreaterThan = 10;

        public const Opcode OpMinus = 11;
        public const Opcode OpBang = 12;

        public const Opcode OpJumpNotTruthy = 13;
        public const Opcode OpJump = 14;

        public const Opcode OpNull = 15;

        public const Opcode OpGetGlobal = 16;
        public const Opcode OpSetGlobal = 17;

        public const Opcode OpArray = 18;
        public const Opcode OpHash = 19;
        public const Opcode OpIndex = 20;

        public const Opcode OpCall = 21;

        public const Opcode OpReturnValue = 22;
        public const Opcode OpReturn = 23;

        public const Opcode OpGetLocal = 24;
        public const Opcode OpSetLocal = 25;

        public const Opcode OpGetBuiltin = 26;

        public const Opcode OpClosure = 27;

        public const Opcode OpGetFree = 28;

        public const Opcode OpCurrentClosure = 29;

        public class Definition
        {
            public string Name;
            public int[] OperandWidths;

        }

        static Dictionary<Opcode, Definition> definitions = new Dictionary<Opcode, Definition>
        {
            { OpConstant, new Definition {Name = "OpConstant", OperandWidths = new int[] {2} } },

            { OpAdd, new Definition {Name = "OpAdd", OperandWidths = new int[] { } } },

            { OpPop, new Definition {Name = "OpPop", OperandWidths = new int[] { } } },

            { OpSub, new Definition {Name = "OpSub", OperandWidths = new int[] { } } },
            { OpMul, new Definition {Name = "OpMul", OperandWidths = new int[] { } } },
            { OpDiv, new Definition {Name = "OpDiv", OperandWidths = new int[] { } } },

            { OpTrue, new Definition {Name = "OpTrue", OperandWidths = new int[] { } } },
            { OpFalse, new Definition {Name = "OpFalse", OperandWidths = new int[] { } } },

            { OpEqual, new Definition {Name = "OpEqual", OperandWidths = new int[] { } } },
            { OpNotEqual, new Definition {Name = "OpNotEqual", OperandWidths = new int[] { } } },
            { OpGreaterThan, new Definition {Name = "OpGreaterThan", OperandWidths = new int[] { } } },

            { OpMinus, new Definition {Name = "OpMinus", OperandWidths = new int[] { } } },
            { OpBang, new Definition {Name = "OpBang", OperandWidths = new int[] { } } },

            { OpJumpNotTruthy, new Definition {Name = "OpJumpNotTruthy", OperandWidths = new int[] {2} } },
            { OpJump, new Definition {Name = "OpJump", OperandWidths = new int[] {2} } },

            { OpNull, new Definition {Name = "OpNull", OperandWidths = new int[] { } } },

            { OpGetGlobal, new Definition {Name = "OpGetGlobal", OperandWidths = new int[] {2} } },
            { OpSetGlobal, new Definition {Name = "OpSetGlobal", OperandWidths = new int[] {2} } },

            { OpArray, new Definition {Name = "OpArray", OperandWidths = new int[] {2} } },
            { OpHash, new Definition {Name = "OpHash", OperandWidths = new int[] {2} } },
            { OpIndex, new Definition {Name = "OpIndex", OperandWidths = new int[] { } } },

            { OpCall, new Definition {Name = "OpCall", OperandWidths = new int[] {1} } },

            { OpReturnValue, new Definition {Name = "OpReturnValue", OperandWidths = new int[] { } } },
            { OpReturn, new Definition {Name = "OpReturn", OperandWidths = new int[] {} } },

            { OpGetLocal, new Definition {Name = "OpGetLocal", OperandWidths = new int[] {1} } },
            { OpSetLocal, new Definition {Name = "OpSetLocal", OperandWidths = new int[] {1} } },

            { OpGetBuiltin, new Definition {Name = "OpGetBuiltin", OperandWidths = new int[] {1} } },

            { OpClosure, new Definition {Name = "OpClosure", OperandWidths = new int[] {2, 1} } },

            { OpGetFree, new Definition {Name = "OpGetFree", OperandWidths = new int[] {1} } },

            { OpCurrentClosure, new Definition {Name = "OpCurrentClosure", OperandWidths = new int[] { } } },
        };


        public static Definition Lookup(byte op, out error _err)
        {
            Definition def;
            if (!definitions.TryGetValue((Opcode)op, out def))
            {
                _err = string.Format("opcode {0:D} undefined", op);
                return null;
            }

            _err = null;
            return def;
        }

        public static List<byte> Make(Opcode op, params int[] operands)
        {
            Definition def;
            if (!definitions.TryGetValue(op, out def))
            {
                return new List<byte> { };
            }

            int instructionLen = 1;
            foreach (int w in def.OperandWidths)
            {
                instructionLen += w;
            }

            List<byte> instruction = new List<byte>(new byte[instructionLen]);
            instruction[0] = (byte)op;

            int offset = 1;
            for (int i = 0; i < operands.Length; i++)
            {
                int o = operands[i];
                int width = def.OperandWidths[i];
                switch (width)
                {
                    case 2:
                        ushort _o = (ushort)o;
                        instruction[offset] = (byte)((_o >> 8) & 0xff);
                        instruction[offset + 1] = (byte)(_o & 0xff);
                        break;
                    case 1:
                        instruction[offset] = (byte)o;
                        break;
                }
                offset += width;
            }

            return instruction;
        }

        public static int[] ReadOperands(Definition def, Instructions ins, int _ins_offset, out int offset)
        {
            int[] operands = new int[def.OperandWidths.Length];
            offset = 0;

            for (int i = 0; i < def.OperandWidths.Length; i++)
            {
                int width = def.OperandWidths[i];
                switch (width)
                {
                    case 2:
                        operands[i] = (int)ReadUint16(ins, offset + _ins_offset);
                        break;
                    case 1:
                        operands[i] = (int)ReadUint8(ins, offset + _ins_offset);
                        break;
                }

                offset += width;
            }

            return operands;
        }

        public static byte ReadUint8(Instructions ins, int offset) { return (byte)ins[offset]; }

        public static ushort ReadUint16(Instructions ins, int offset)
        {
            return (ushort)((ins[offset] << 8) | ins[offset + 1]);
        }

    }
}
