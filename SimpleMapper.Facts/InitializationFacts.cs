using System;
using Moq;
using Ploeh.AutoFixture.Xunit;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{

    public class GenericFacts{

        [Fact]
        public void SetupMapShouldBePossibleToParsePropertyNamesFromLambdas(){
            
            //Mapper.SetupMapping.SetupMap.GetPropertyNameFromLambda(x)

        }
    }

    public class IntializationFacts
    {
        [Fact]
        public void ShouldBePossibleToInitializeSharedConfig()
        {
            ObjectMapper.Configure(new MapperConfiguration
                                   {
                                       CreateMissingMapsAutomaticly = true,
                                       CustomActivator = null
                                   });

            Assert.Equal(ObjectMapper.CurrentConfiguration.CustomActivator, null);
            Assert.Equal(ObjectMapper.CurrentConfiguration.CreateMissingMapsAutomaticly, true);
        }

        [Theory, AutoData]
        public void ShouldInitializeConfigurationOnceSet(Mock<IMapperConfiguration> configurationMock)
        {
            ObjectMapper.Configure(configurationMock.Object);

            configurationMock.Verify(x => x.Initialize(), Times.Once());
        }
    }    
}
