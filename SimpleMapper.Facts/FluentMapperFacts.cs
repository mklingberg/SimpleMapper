using System.Globalization;
using System.Linq;
using Moq;
using Ploeh.AutoFixture.Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class FluentMapperFacts
    {
        [Theory, AutoData]
        public void Mappings([Frozen] Mock<IMapperConfiguration> configurationMock){
            var map = new Mapper.SetupMapping(configurationMock.Object);

            map.UsingConvention(
                (s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new {source, destination});
            
            map.FromTo<ClassAModel, ClassA>();
            map.FromTo<ClassAModel, ClassA>().Set(x => x.P1, x => x.P2);
            map.From<ClassAModel>().To<ClassA>().Ignore(x => x.P1);

            map.From<ClassA>().To<ClassAModel>()
                .IncludeFrom<ClassB>()
                .SetManually((s, d) => {
                    d.P1 = s.P1;
                    d.P2 = s.P2;
                    d.P3 = s.P3;
                });

            map.From<ClassAModel>().To<ClassA>()
                .CreateWith(x => new ClassA { P4 = "Hej" })
                .Ignore(x => x.P3)
                .SetManually((s, d) => {
                    d.P1 = s.P1;
                    d.P2 = s.P2;
                    d.P3 = s.P3;
                });

            map.From<ClassA>().To<ClassAModel>()
                .AddCustomConvention((s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new {source, destination})
                .AddCustomConversion<int, string>(i => i.ToString(CultureInfo.CurrentCulture))
                .Set(x => x.P1)
                .SetManually((s, d) => {
                    d.P1 = s.P1;
                    d.P2 = s.P2;
                    d.P3 = s.P3;
                });

        }
    }

    public class ClassA {
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
    }

    public class ClassAModel {
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
    }
}
