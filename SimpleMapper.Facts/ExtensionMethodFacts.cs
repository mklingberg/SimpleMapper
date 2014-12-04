using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class ExtensionMethodFacts
    {
        [Theory, AutoTestData]
        public void ShouldBePossibleToJustMapFromClassAToItsModelWithDefaultConvention(ClassA classA){
            var model = classA.MapTo<ClassAModel>();

            Assert.True(classA.P1 == model.P1);
            Assert.True(classA.P2 == model.P2);
            Assert.True(classA.P3 == model.P3);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToMapFromExistingClass(P1Class p1, P1P4Model model){
            model.MapFrom(p1);
            Assert.True(p1.P1 == model.P1);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToMapFromExistingClasses(P1Class p1, P2Class p2, P1P4Model model){
            model.MapFrom(p1, p2);

            Assert.True(p1.P1 == model.P1);
            Assert.True(p2.P2 == model.P2);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToMapAllFromList(List<P1Class> sources){
            var models = sources.MapTo<P1P4Model>().ToList();

            for (var i = 0; i < sources.Count; i++){
                Assert.True(sources[i].P1 == models[i].P1);
            }
        }

        public class P1P4Model{
            public string P1 { get; set; }
            public string P2 { get; set; }
        }

        public class P1Class{
            public string P1 { get; set; }
        }

        public class P2Class{
            public string P2 { get; set; }
        }
    }
}
