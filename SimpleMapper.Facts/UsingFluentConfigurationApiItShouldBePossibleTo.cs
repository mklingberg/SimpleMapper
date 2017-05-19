using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Moq;
using Ploeh.AutoFixture.Xunit;
using SimpleMapper.Facts.AutoFixture;
using SimpleMapper.Facts.TestObjects;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class UsingFluentConfigurationApiItShouldBePossibleTo
    {
        [Theory, AutoTestData]
        public void AddConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.WithConvention(
                (s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new { source, destination });

            configurationMock.Verify(x => x.AddConvention(It.IsAny<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void AddGenericConvention([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.WithConvention<SameNameIgnoreCaseConvention>();

            configurationMock.Verify(x => x.AddConvention(It.IsAny<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void AddManualMapWithFromTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.FromTo<ClassAModel, ClassA>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void AddManualMapWithFromAndThenTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.From<ClassAModel>().To<ClassA>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Once);
        }

        [Theory, AutoTestData]
        public void SetPropertiesToMapWithConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.FromTo<ClassAModel, ClassA>().Set(x => x.P1, x => x.P2);

            Assert.Contains("P3", manualMap.IgnoreProperties);
            Assert.Contains("P4", manualMap.IgnoreProperties);
        }

        [Theory, AutoTestData]
        public void SetPropertiesToIgnoreWhenMappingWithConventions([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.FromTo<ClassAModel, ClassA>().Ignore(x => x.P1, x => x.P2);

            Assert.Contains("P1", manualMap.IgnoreProperties);
            Assert.Contains("P2", manualMap.IgnoreProperties);
        }

        [Theory, AutoTestData]
        public void SetManualMap([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.From<ClassAModel>().To<ClassA>()
                .SetManually((s, d) =>
                {
                    d.P1 = s.P1;
                    d.P2 = s.P2;
                    d.P3 = s.P3;
                });

            Assert.NotNull(manualMap.ObjectMap);
        }

        [Theory, AutoTestData]
        public void AddMapsForInheritedClassesWithIncludeFrom([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.From<ClassA>().To<ClassAModel>().IncludeFrom<ClassB>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Exactly(2));
        }

        [Theory, AutoTestData]
        public void AddMapsForInheritanceWithIncludeTo([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            map.From<ClassAModel>().To<ClassA>().IncludeTo<ClassB>();

            configurationMock.Verify(x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()), Times.Exactly(2));
        }


        [Theory, AutoTestData]
        public void AddCustomConvention([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.From<ClassAModel>().To<ClassA>()
                .WithCustomConvention((s, d) =>
                    from destination in d
                    join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                    where source.CanRead && destination.CanWrite
                    select new { source, destination });

            Assert.True(manualMap.Conventions.Count == 1);
        }

        [Theory, AutoTestData]
        public void AddCustomConventionUsingGenerics([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.From<ClassAModel>().To<ClassA>().WithCustomConvention<SameNameIgnoreCaseConvention>();

            Assert.True(manualMap.Conventions.Count == 1);
        }

        [Theory, AutoTestData]
        public void AddCustomConversion([Frozen] Mock<IMapperConfiguration> configurationMock, Mapper.SetupMapping map)
        {
            ManualMap<ClassAModel, ClassA> manualMap = null;

            configurationMock.Setup(
                x => x.AddMap(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<IPropertyMap>()))
                .Callback<Type, Type, IPropertyMap>((a, b, c) => manualMap = (ManualMap<ClassAModel, ClassA>)c);

            map.From<ClassAModel>().To<ClassA>()
                .WithCustomConversion<int, string>(i => i.ToString(CultureInfo.CurrentCulture));

            Assert.True(manualMap.Conversions.Count == 1);
        }

        //[Theory, AutoTestData]
        //public void AddConventionFromMapper([Frozen] Mock<IMapperConfiguration> configurationMock,
        //    Mapper.SetupConfiguration configure) {

        //    configure.WithConvention((s, d) =>
        //            from destination in d
        //            join source in s on destination.Name.ToLower() equals source.Name.ToLower()
        //            where source.CanRead && destination.CanWrite
        //            select new{source, destination});

        //    //PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>

        //    Assert.True(configurationMock.Verify(x => x.AddConvention(It.IsAny<PropertyInfo[]>(), It.IsAny<PropertyInfo[]>(), It.IsAny<IEnumerable<object>>()), Times.Once));

        //}
    }
}