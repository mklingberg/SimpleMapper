using Ploeh.AutoFixture.Xunit;
using SimpleMapper.Facts.TestObjects;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class DefaultConfigurationShould
    {
        [Theory, AutoData]
        internal void HaveBuiltinSameNameConvention(SameName source)
        {
            Assert.Equal(source.MapTo<SameNameDto>().SomeProperty, source.SomeProperty);
        }
    }

    public class TestFluentConfiguration
    {
        [Theory, AutoData]
        internal void VerifyIgnoreMapping(TestIgnore1 source)
        {
            Assert.NotEqual(source.MapTo<TestIgnore2>().MyProperty, source.MyProperty);
        }

        [Theory, AutoData]
        internal void VerifyComplexConversionForInterfaces(WithNamedEntityProperty source, WithNamedEntityStringProperty destination)
        {
            Assert.Equal(source.MapTo<WithNamedEntityStringProperty>().NamedEntity, source.NamedEntity.Name);
        }
    }
}