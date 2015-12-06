using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Xunit;

namespace SimpleMapper.Facts.AutoFixture
{
    public class AutoTestDataAttribute : AutoDataAttribute
    {
        public AutoTestDataAttribute() : base(new Fixture().Customize(new AutoMoqCustomization()).Customize(new MultipleCustomization()))
        { }
    }
}