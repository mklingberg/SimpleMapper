using System.Collections.Generic;

namespace SimpleMapper.Facts.TestObjects{
    public class RecursiveClass1Model
    {
        public string Data { get; set; }
        public RecursiveClass2Model OtherClass { get; set; }
        public List<RecursiveClass2Model> ClassList { get; set; }
    }
}