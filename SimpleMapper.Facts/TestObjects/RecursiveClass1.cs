using System.Collections.Generic;

namespace SimpleMapper.Facts.TestObjects{
    public class RecursiveClass1
    {
        public string Data { get; set; }
        public RecursiveClass2 OtherClass { get; set; }
        public List<RecursiveClass2> ClassList { get; set; }
    }
}