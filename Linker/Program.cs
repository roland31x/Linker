using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linker
{
    internal class Program
    {
        static void Main(string[] args)
        {
        }
    }
    class MemoryMap
    {
        public List<int> Map { get; set; }

    }
    class MemoryTable
    {
        public List<Memory> Table { get; set; }
        public MemoryTable(Module m)
        {
            
        }

        public void AddMem()
        {

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
    }
    class Module
    {
        public List<int> OpList { get; set; }
        public List<Memory> DefList { get; set; }
        public List<Memory> UseList { get; set; }

        public int Length { get; set; }
        public Module()
        {
            OpList = new List<int>();
            DefList = new List<Memory>();
            UseList = new List<Memory>();
            Length = OpList.Count;
        }
    }
}
