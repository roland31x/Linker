using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linker
{
    class Linker
    {
        readonly string path = "input";
        readonly string opath = "output";
        StreamReader sr { get; set; }
        StreamWriter sw { get; set; }

        MemoryTable MT { get; set; }

        MemoryMap MP { get; set; }

        List<Module> modules { get; set; }

        readonly static char[] sep = new char[] { ' ' };

        public Linker()
        {
            InitializeOutput();
            modules = new List<Module>();
            MT = new MemoryTable();
            MP = new MemoryMap();
            sr = new StreamReader(path);
            sw = new StreamWriter(opath, true);
        }
        void InitializeOutput()
        {
            if (File.Exists(opath))
            {
                File.Delete(opath);
            }
            File.Create(opath).Close();
        }
        public void WriteInfo()
        {
            //Console.WriteLine();
            //Console.WriteLine("Memory Table:");
            //foreach(DefinedMemory dm in MT.Table)
            //{
            //    Console.WriteLine(dm.ToString());
            //}
            //Console.WriteLine();
            //Console.WriteLine("Memory map:");
            //for(int i = 0; i < MP.Map.Count; i++)
            //{
            //    Console.WriteLine($"{i}: {MP.Map[i]}");
            //}

            sw.WriteLine();
            sw.WriteLine("Symbol Table:");
            foreach (DefinedMemory dm in MT.Table)
            {
                sw.WriteLine(dm.ToString());
            }
            sw.WriteLine();
            sw.WriteLine("Memory map:");
            for (int i = 0; i < MP.Map.Count; i++)
            {
                sw.WriteLine($"{i}: {MP.Map[i]}");
            }

            sw.Close();
        }
        public void SecondPass()
        {
            UseCheck(); // sets the useage of operations for each variable
            Solve();
        }
        public void UseCheck() // checks usage of each memory fragment in operations and assigns their use
        {
            foreach (Module M in modules)
            {
                foreach (Memory m in M.UseList)
                {
                    int use = M.OpList[m.Pos].Field;
                    if (CheckUse(M.OpList[m.Pos]))
                    {
                        continue;
                    }
                    CheckImm(M.OpList[m.Pos]);
                    M.OpList[m.Pos].IsUsedBy = m;
                    while (use != 777)
                    {
                        if (CheckUse(M.OpList[use]))
                        {
                            // now here idk if the chain of uses for that memory fragment should continue or not, i assume it does 
                            use = M.OpList[use].Field;
                            continue;
                        }
                        CheckImm(M.OpList[use]);
                        M.OpList[use].IsUsedBy = m;
                        use = M.OpList[use].Field;
                    }
                }
            }
        }
        bool CheckUse(OP op)
        {
            if (op.IsUsedBy != null)
            {
                //Console.WriteLine($"Error: Operation {op} is already used by {op.IsUsedBy.ID}.");
                sw.WriteLine($"Error: Operation {op} is already used by {op.IsUsedBy.ID}.");
                return true;
            }
            else return false;
        }
        void CheckImm(OP op)
        {
            if (op.Type == 1)
            {
                //Console.WriteLine($"Error: Operation {op} is of type immediate and is used by a variable, it will be treated as an external");
                sw.WriteLine($"Error: Operation {op} is of type immediate and is used by a variable, it will be treated as an external");
                op.Type = 4;
            }
        }
        void Solve()
        {
            foreach (Module M in modules)          // modules are in order so it's ok to go through them, espeically when i have to check for relative address
            {
                foreach (OP op in M.OpList)
                {
                    ResolveOp(op, M);
                }
            }
        }
        void ResolveOp(OP op, Module M)
        {
            switch (op.Type)
            {
                case 1:
                    Immediate(op); break;
                case 2:
                    Absolute(op); break;
                case 3:
                    Relative(op, M); break;
                case 4:
                    External(op); break;
                default: break;
            }
        }
        void Immediate(OP op)   // i don't know what the difference between immediate and absolute is
        {
            op.Value = op.Value / 10;
        }
        void Absolute(OP op)
        {
            //if(op.Field > 199)   // if absolute is bigger than machine size ( 200 words ) // yeah idk how this works
            //{
            //    op.Field = 199;
            //    op.Value = 1000 * op.OpCode + op.Field;
            //    return;
            //}
            op.Value = op.Value / 10;
        }
        void Relative(OP op, Module M)
        {
            if (op.IsUsedBy != null)
            {
                External(op);
                op.Value += M.Offset;
                return;
            }
            op.Value = (op.Value / 10) + M.Offset;
        }
        void External(OP op)
        {
            int used = -1;
            foreach (DefinedMemory dm in MT.Table)
            {
                try
                {
                    if (dm.ID == op.IsUsedBy.ID)
                    {
                        used = dm.Address;
                    }
                }
                catch (NullReferenceException) // this means the op itself is marked as external but was never assigned a variable but uses it, so isUsedBy will be null
                {
                    //Console.WriteLine($"Error: Operation {op} is not used by any variable, will be treated as immediate address.");
                    sw.WriteLine($"Error: Operation {op} is not used by any variable, will be treated as immediate address.");
                    op.Type = 1;
                    Immediate(op); // if its not used by any of the declared outside variables it will be treated as an immediate address
                    return;
                }
            }
            StringBuilder str = new StringBuilder();
            str.Append(op.OpCode);
            string toadd = used.ToString();
            for (int i = toadd.Length; i < 3; i++)
            {
                str.Append('0');
            }
            str.Append(toadd);
            op.Value = int.Parse(str.ToString());
        }
        // FIRSTPASS FROM HERE
        public void FirstPass()
        {
            string s = sr.ReadLine().Trim();
            int n = int.Parse(s);
            sr.ReadLine(); // clears line after module count           
            for (int i = 1; i <= n; i++)
            {
                Module M = new Module(i);

                ReadDefList(sr, M);
                ReadUseList(sr, M);
                ReadOpList(sr, M);

                modules.Add(M);

                sr.ReadLine(); // clears empty line after a module
            }

            OffsetCalc(); // calculates the module offsets

            MemTableCalc(); // calculates all the variables declared and their declaration address

            InitialMemMap(); // maps the operations to the mem map without solving anything yet

            sr.Close(); // doesn't need to read from file anymore so streamreader can be closed, everything is stored in this object
        }


        // PASS 1 FUNCTIONS
        void InitialMemMap()
        {
            foreach (Module M in modules)
            {
                foreach (OP f in M.OpList)
                {
                    MP.Map.Add(f);
                }
            }
        }
        void MemTableCalc()
        {
            foreach (Module M in modules)
            {
                foreach (Memory mem in M.DefList)
                {
                    if (mem.Pos >= M.OpList.Count)
                    {
                        //Console.WriteLine($"Error: {mem} definition is out of module bounds, it's address has been set to zero.");
                        sw.WriteLine($"Error: {mem} definition is out of module bounds, it's address has been set to zero (relative).");
                        MT.Table.Add(new DefinedMemory(0 + M.Offset, mem.ID));
                        continue;
                    }
                    MT.Table.Add(new DefinedMemory(mem.Pos + M.Offset, mem.ID));
                }
            }

            for (int i = 0; i - 1 < MT.Table.Count; i++)   // check for multiple definitions
            {
                for (int j = i + 1; j < MT.Table.Count; j++)
                {
                    if (MT.Table[i].ID == MT.Table[j].ID)
                    {
                        //Console.WriteLine($"Error: {MT.Table[i]} defined in multiple cases, will only use first instance");
                        sw.WriteLine($"Error: {MT.Table[i]} defined in multiple cases, will only use first instance");
                        MT.Table.Remove(MT.Table[j]);
                    }
                }
            }

            foreach (DefinedMemory dm in MT.Table)   // check for usage 
            {
                bool ok = false;
                foreach (Module M in modules)
                {
                    foreach (Memory m in M.UseList)
                    {
                        if (dm.ID == m.ID)
                        {
                            ok = true;
                        }
                    }
                }
                if (!ok)
                {
                    //Console.WriteLine($"Warning: {dm.ID} was declared but never used!");
                    sw.WriteLine($"Warning: {dm.ID} was declared but never used!");
                }
            }

            foreach (Module M in modules)    // check for undeclared variables
            {
                foreach (Memory m in M.UseList)
                {
                    bool ok = false;
                    foreach (DefinedMemory dm in MT.Table)
                    {
                        if (dm.ID == m.ID)
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        //Console.WriteLine($"Warning {m.ID} was never declared, will have it's default value at 0!");
                        sw.WriteLine($"Error: {m.ID} was never declared, will be declared with its address at 0!");
                        bool isDupe = false;                                  // could have multiple undeclared variables of same name, only adds one.
                        foreach (DefinedMemory checkifdupe in MT.Table)
                        {
                            if (checkifdupe.ID == m.ID)
                            {
                                isDupe = true; break;
                            }
                        }
                        if (!isDupe)
                        {
                            MT.Table.Add(new DefinedMemory(0, m.ID));
                        }
                    }
                }
            }
        }
        void OffsetCalc()
        {
            int i = 0;
            foreach (Module m in modules)
            {
                m.Offset = i;
                foreach (OP f in m.OpList)
                {
                    i++;
                }
            }
        }
        void ReadDefList(StreamReader s, Module m)
        {
            string line = s.ReadLine().Trim();
            int nr = 0;
            if (line[0] == '0')
            {
                return;
            }
            else nr = line[0] - '0';

            string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i + 1 < tokens.Length; i += 2)
            {
                m.DefList.Add(new Memory(tokens[i], int.Parse(tokens[i + 1])));
            }
        }
        void ReadUseList(StreamReader s, Module m)
        {
            string line = s.ReadLine().Trim();
            int nr = 0;
            if (line[0] == '0')
            {
                return;
            }
            else nr = line[0] - '0';

            string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i + 1 < tokens.Length; i += 2)
            {
                m.UseList.Add(new Memory(tokens[i], int.Parse(tokens[i + 1])));
            }
        }
        void ReadOpList(StreamReader s, Module m)
        {
            string line = s.ReadLine().Trim();
            int nr = 0;
            if (line[0] == '0')
            {
                return;
            }
            else nr = line[0] - '0';

            string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < tokens.Length; i++)
            {
                m.OpList.Add(new OP(int.Parse(tokens[i])));
            }
        }
        // PASS 1 FUNCTIONS ABOVE
    }
    class MemoryMap
    {
        public List<OP> Map { get; set; }

        public MemoryMap()
        {
            Map = new List<OP>();
        }

    }
    class MemoryTable
    {
        public List<DefinedMemory> Table { get; set; }
        public MemoryTable()
        {
            Table = new List<DefinedMemory>();
        }
    }
    class DefinedMemory
    {
        public string ID { get; set; }

        public int Address { get; set; }

        public DefinedMemory(int add, string id)
        {
            ID = id;
            Address = add;
        }
        public override string ToString()
        {
            return $"{ID} : {Address}";
        }
    }
    class Memory
    {
        public string ID { get; set; }
        public int Pos { get; set; }

        public Memory(string id, int value)
        {
            ID = id; Pos = value;
        }
        public override string ToString()
        {
            return $"{ID}";
        }
    }
    class OP
    {
        public int Value { get; set; }
        public int OpCode { get; set; }
        public int Field { get; set; }
        public int Type { get; set; }

        public Memory IsUsedBy { get; set; }
        public OP(int i)
        {
            OpCode = i / 10000;
            Field = (i - ((i / 10000) * 10000)) / 10;
            Type = i % 10;
            Value = i;
        }
        public override string ToString()
        {
            return $"{Value}";
        }
        public string Resolved()
        {
            return $"{Value / 10}";
        }
    }
    class Module
    {
        public int Pos { get; set; }

        public int Offset { get; set; }
        public List<OP> OpList { get; set; }
        public List<Memory> DefList { get; set; }
        public List<Memory> UseList { get; set; }

        public Module(int pos)
        {
            OpList = new List<OP>();
            DefList = new List<Memory>();
            UseList = new List<Memory>();
            Pos = pos;
            Offset = 0;
        }
        public override string ToString()
        {
            return $"M{Pos}";
        }
    }
}
