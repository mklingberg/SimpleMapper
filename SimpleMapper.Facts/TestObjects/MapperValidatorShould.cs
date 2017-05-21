using Xunit;

namespace SimpleMapper.Facts.TestObjects
{
    public class MapperValidatorShould
    {
        [Fact]
        public void ThrowExceptionOnSomePropertyNotConfiguredOrMapped()
        {
            var configuration = new MapperConfiguration {Scanner = {Enabled = false}};

            var setupMap = new Mapper.SetupMapping(configuration);
            configuration.AddConvention<SameNameIgnoreCaseConvention>();

            setupMap.FromTo<ValidatorTest1, ValidatorTest1Model>();

            configuration.Initialize();

            Assert.Throws<SomePropertiesNotMappedException>(() => configuration.AssertAllPropertiesMappedOnDestinationObjects());
        }

        [Fact]
        public void NotThrowExceptionOnAllPropertiesConfigured()
        {
            var configuration = new MapperConfiguration { Scanner = { Enabled = false } };

            var setupMap = new Mapper.SetupMapping(configuration);
            configuration.AddConvention<SameNameIgnoreCaseConvention>();

            setupMap.FromTo<ValidatorTest1, ValidatorTest1Model>().Ignore(x => x.OtherProperty);

            configuration.Initialize();

            configuration.AssertAllPropertiesMappedOnDestinationObjects();
        }

        [Fact]
        public void ThrowExceptionOnPropertyNotMappedOnManualMap()
        {
            var configuration = new MapperConfiguration { Scanner = { Enabled = false } };

            var setupMap = new Mapper.SetupMapping(configuration);
            configuration.AddConvention<SameNameIgnoreCaseConvention>();

            setupMap.FromTo<ValidatorTest1, ValidatorTest1Model>().SetManually((s, d) => {
                d.OtherProperty = "Hejsan";
            });

            configuration.Initialize();

            Assert.Throws<SomePropertiesNotMappedException>(() => configuration.AssertAllPropertiesMappedOnDestinationObjects());
        }

        [Fact]
        public void NotThrowExceptionWhenManualSetHasBeenSpecified()
        {
            var configuration = new MapperConfiguration { Scanner = { Enabled = false } };

            var setupMap = new Mapper.SetupMapping(configuration);
            configuration.AddConvention<SameNameIgnoreCaseConvention>();

            setupMap.FromTo<ValidatorTest1, ValidatorTest1Model>().SetManually((s, d) => {
                d.OtherProperty = "Hejsan";
            }, x => x.OtherProperty);

            configuration.Initialize();

            configuration.AssertAllPropertiesMappedOnDestinationObjects();
        }

        [Fact]
        public void ThrowExceptionOnInitializeForMissingTypeMapWithAutomapDisabled()
        {
            var configuration = new MapperConfiguration { Scanner = { Enabled = false }, CreateMissingMapsAutomatically = false };

            var setupMap = new Mapper.SetupMapping(configuration);
            configuration.AddConvention<SameNameIgnoreCaseConvention>();

            setupMap.FromTo<ValidatorTest1, ValidatorTest1Model>().SetManually((s, d) => {
                d.OtherProperty = "Hejsan";
            }, x => x.OtherProperty);

            Assert.Throws<MapNotConfiguredException>(() => configuration.Initialize());
            Assert.Throws<MapperException>(() => configuration.AssertAllPropertiesMappedOnDestinationObjects());
        }
    }

    public class ValidatorTest1
    {
        public string Name { get; set; }

        public Customer Customer { get; set; }

    }

    public class ValidatorTest1Model
    {
        public string Name { get; set; }
        public string OtherProperty { get; set; }

        public CustomerModel Customer { get; set; }

    }
}
