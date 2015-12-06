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
}