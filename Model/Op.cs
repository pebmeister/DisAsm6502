using System;
using System.Collections.ObjectModel;

namespace DisAsm6502.Model
{
    [Serializable]
    public enum AddressingModes
    {
        I = 0,  // implied
        Im,     // immediate
        Zp,     // zero page
        Zpi,    // zero page indirect
        Zpx,    // zero page x
        Zpy,    // zero page y
        Zpix,   // zero page indirect x
        Zpiy,   // zero page indirect y
        A,      // absolute
        Aix,    // absolute indirect x
        Ax,     // absolute x
        Ay,     // absolute y
        Ind,    // absolute indirect
        R,      // relative
        Zr,     // zero page relative
        Ac,     // Accumulator
        MaxAddressingMode
    }

    /// <summary>
    /// class to hold string representation of opcode
    /// and addressing mode
    /// </summary>
    [Serializable]
    public class Op
    {
        public string Opcode;
        public AddressingModes Mode;
        public Op(string op, AddressingModes m)
        {
            Opcode = op;
            Mode = m;
        }

        public Op()
        {
            Opcode = "";
            Mode = AddressingModes.I;
        }
    }

    [Serializable]
    public class OpCollection
    {
        public Collection<Op> Ops = new Collection<Op>();
    }

    [Serializable]
    public class Sym
    {
        public int Address;
        public string Name;
    }

    [Serializable]
    public class SymCollection
    {
        public Collection<Sym> Symbols = new Collection<Sym>();
    }
}

