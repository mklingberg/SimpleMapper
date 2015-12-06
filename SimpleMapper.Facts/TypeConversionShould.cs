using System;
using System.Globalization;
using Ploeh.AutoFixture.Xunit;
using SimpleMapper.Facts.AutoFixture;
using SimpleMapper.Facts.TestObjects;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class TypeConversionShould
    {
        [Theory, AutoData]
        internal void ConvertDateTimeToString(DateTimeClass source)
        {
            Assert.Equal(source.MapTo<DateTimeClassDto>().Date, source.Date.ToString(CultureInfo.CurrentUICulture));
        }

        [Theory, AutoData]
        internal void ConvertStringToDateTime(DateTimeClassDto source)
        {
            source.Date = DateTime.Now.ToString();
            Assert.Equal(source.MapTo<DateTimeClass>().Date, Convert.ToDateTime(source.Date));
        }

        [Theory, AutoData]
        internal void ConvertIntToString(NumberClass source)
        {
            Assert.Equal(source.MapTo<NumberClassDto>().Count, source.Count.ToString());
        }

        [Theory, AutoData]
        internal void ConvertStringToInt(NumberClassDto source)
        {
            source.Count = "199";
            Assert.Equal(source.MapTo<NumberClass>().Count, Convert.ToInt32(source.Count));
        }

        [Theory, AutoTestData]
        internal void ConvertEnumsWithSameValue(EnumEntityA entityA)
        {
            entityA.Enum1 = Enum1.B;
            var entityB = entityA.MapTo<EnumEntityB>();
            Assert.True(entityB.Enum1.ToString() == entityA.Enum1.ToString());
        }

        [Theory, AutoTestData]
        internal void ThrowExceptionWhenConvertingToEnumThatLacksSourceValue(EnumEntityB entityA)
        {
            entityA.Enum1 = Enum2.E;
            Assert.Throws<MapperException>(() => entityA.MapTo<EnumEntityA>());
        }

        [Theory, AutoTestData]
        internal void ConvertBoolToString(BoolClassA boolClass)
        {
            Assert.True(boolClass.MapTo<BoolClassAModel>().Flag == boolClass.Flag.ToString());
        }

        [Theory, AutoTestData]
        internal void ConvertStringToBool(BoolClassAModel model)
        {
            model.Flag = "true";
            Assert.True(model.MapTo<BoolClassA>().Flag);
        }

        [Theory, AutoTestData]
        internal void ThrowExceptionWhenDetectingInfiniteLoops()
        {
            var parent = new ParentClass { ChildObject = new ChildClass() };
            parent.ChildObject.ParentObject = parent;

            Assert.Throws<MapperException>(() => parent.MapTo<ParentClassModel>());
        }

        [Theory, AutoTestData]
        internal void MapChildObjectsAndListsRecursively(RecursiveClass1 source)
        {
            var model = source.MapTo<RecursiveClass1Model>();

            Assert.True(model.ClassList.Count == source.ClassList.Count);
            Assert.True(model.OtherClass.DataValue == source.OtherClass.DataValue);
        }
    }
}