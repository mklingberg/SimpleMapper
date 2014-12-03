using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Moq;
using Ploeh.AutoFixture.Xunit;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts{
    public class FluentConfigurationTheories {
        [Theory, AutoTestData]
        public void ShouldBePossibleToAddConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            map.WithConvention(
                (s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new{source, destination});

            configurationMock.Verify(x => x.AddConvention(It.IsAny<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToAddManualMapWithFromTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            map.FromTo<ClassAModel, ClassA>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToAddManualMapWithFromAndThenTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            map.From<ClassAModel>().To<ClassA>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToSetPropertiesToMapWithConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a,b,c) => manualMap = (ManualMap<ClassAModel, ClassA>) c);

            map.FromTo<ClassAModel, ClassA>().Set(x => x.P1, x => x.P2);
            
            Assert.Contains("P3", manualMap.IgnoreProperties);
            Assert.Contains("P4", manualMap.IgnoreProperties);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToSetPropertiesToIgnoreWhenMappingWithConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a,b,c) => manualMap = (ManualMap<ClassAModel, ClassA>) c);

            map.FromTo<ClassAModel, ClassA>().Ignore(x => x.P1, x => x.P2);

            Assert.Contains("P1", manualMap.IgnoreProperties);
            Assert.Contains("P2", manualMap.IgnoreProperties);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToSetManualMap([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a,b,c) => manualMap = (ManualMap<ClassAModel, ClassA>) c);

            map.From<ClassAModel>().To<ClassA>()
                .SetManually((s, d) =>{
                                 d.P1 = s.P1;
                                 d.P2 = s.P2;
                                 d.P3 = s.P3;
                             });

            Assert.NotNull(manualMap.ObjectMap);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToAddMapsForInheritedClassesWithIncludeFrom([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            map.From<ClassA>().To<ClassAModel>().IncludeFrom<ClassB>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Exactly(2));
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToAddMapsForInheritanceWithIncludeTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            map.From<ClassAModel>().To<ClassA>().IncludeTo<ClassB>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Exactly(2));
        }


        [Theory, AutoTestData]
        public void ShouldBePossibleToAddCustomConvention([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a,b,c) => manualMap = (ManualMap<ClassAModel, ClassA>) c);

            map.From<ClassAModel>().To<ClassA>()
                .WithCustomConvention((s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new{source, destination});

            Assert.True(manualMap.Conventions.Count == 1);
        }

        [Theory, AutoTestData]
        public void ShouldBePossibleToAddCustomConversion([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map){
            
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a,b,c) => manualMap = (ManualMap<ClassAModel, ClassA>) c);

            map.From<ClassAModel>().To<ClassA>()
                .WithCustomConversion<int, string>(i => i.ToString(CultureInfo.CurrentCulture));

            Assert.True(manualMap.Conversions.Count == 1);
        }
    }

    public class ClassA{
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
    }

    public class ClassAModel{
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
    }
}