using SimpleMapper.Facts.AutoFixture;
using SimpleMapper.Facts.TestObjects;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class UsingLambdaCompilerItShouldBePossibleTo
    {
        [Theory, AutoTestData]
        public void CreateGetter(ClassA classA)
        {
            var getter = LambdaCompiler.CreateGetter<ClassA, string>("P1");
            Assert.True(classA.P1 == getter(classA));
        }

        [Theory, AutoTestData]
        public void CreateSetter(ClassA classA)
        {
            var setter = LambdaCompiler.CreateSetter<ClassA, string>("P1");
            setter(classA, "hej");
            Assert.True(classA.P1 == "hej");
        }
    }
}