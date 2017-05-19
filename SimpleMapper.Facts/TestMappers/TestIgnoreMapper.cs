using SimpleMapper.Facts.TestObjects;

namespace SimpleMapper.Facts.TestMappers
{
    public class TestIgnoreMapper : Mapper
    {
        public TestIgnoreMapper()
        {
            Map.From<TestIgnore1>().To<TestIgnore2>().Ignore(x => x.MyProperty);
        }
    }
}