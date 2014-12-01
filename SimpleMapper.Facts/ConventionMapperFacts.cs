using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class ConventionMapperFacts
    {
        public class SpeedTests
        {
            private const int ENTITY_COUNT = 5000;

            //[Theory, AutoData]
            //public void TestTimeMappingEntititesUsingConventionMapperAndCompareToManualMapping(IFixture fixture)
            //{
            //    var entities = fixture.Build<EntityA>().CreateMany(ENTITY_COUNT).ToList();

            //    ObjectMapper.CreateMap<EntityA, EntityB>();

            //    var timerAuto = new Stopwatch();
            //    var timerManual = new Stopwatch();

            //    timerAuto.Start();

            //    var autoMappedItems = entities.MapTo<EntityB>().ToList();

            //    timerAuto.Stop();

            //    timerManual.Start();

            //    var manuallyMapped = entities.Select(e => new EntityB {
            //                                               P0 = e.P0, P1 = e.P1, P2 = e.P2, P3 = e.P3, P4 = e.P4, P5 = e.P5, P6 = e.P6, P7 = e.P7, P8 = e.P8, P9 = e.P9
            //                                           }).ToList();
            //    timerManual.Stop();

            //    Console.WriteLine("TimeMapper: {0} TimeManual: {1}", timerAuto.ElapsedMilliseconds, timerManual.ElapsedMilliseconds);
            //}

            //[Theory, AutoData]
            //public void TestTimeMappingEntititesUsingManualMapperAndCompareToManualMapping(IFixture fixture)
            //{
            //    var entities = fixture.Build<EntityA>().CreateMany(ENTITY_COUNT).ToList();

            //    ObjectMapper.AddMap<EntityA, EntityB>((s, d) => {
            //                                                           d.P0 = s.P0;
            //                                                           d.P2 = s.P2;
            //                                                           d.P3 = s.P3;
            //                                                           d.P4 = s.P4;
            //                                                           d.P5 = s.P5;
            //                                                           d.P6 = s.P6;
            //                                                           d.P7 = s.P7;
            //                                                           d.P8 = s.P8;
            //                                                           d.P9 = s.P9;
            //                                                       }, false);

            //    var timerAuto = new Stopwatch();
            //    var timerManual = new Stopwatch();

            //    timerAuto.Start();


            //    var autoMappedItems = entities.MapTo<EntityB>().ToList();

            //    timerAuto.Stop();

            //    timerManual.Start();

            //    var manuallyMapped = entities.Select(e => new EntityB {
            //                                               P0 = e.P0, P1 = e.P1, P2 = e.P2, P3 = e.P3, P4 = e.P4, P5 = e.P5, P6 = e.P6, P7 = e.P7, P8 = e.P8, P9 = e.P9
            //                                           }).ToList();
            //    timerManual.Stop();

            //    Console.WriteLine("TimeMapper: {0} TimeManual: {1}", timerAuto.ElapsedMilliseconds, timerManual.ElapsedMilliseconds);
            //}
        }

        //public class TypeResolveFacts
        //{
        //    [Theory, AutoData]
        //    public void ShouldBePossibleToMapPolymorphicModelsAsTypeB(TypeLookupModel model)
        //    {
        //        model.Type = "b";



        //        ObjectMapper.ResolveUsing<TypeLookupModel>(x => x.Type == "a" ? typeof (ClassA) : typeof (ClassB));

                

        //        Assert.IsType<ClassB>(model.MapTo<ClassA>());
        //    }

        //    [Theory, AutoData]
        //    public void ShouldBePossibleToMapPolymorphicModelsAsTypeA(TypeLookupModel model)
        //    {
        //        model.Type = "a";
                
        //        ObjectMapper.ResolveUsing<TypeLookupModel>(x => x.Type == "a" ? typeof (ClassA) : typeof (ClassB));

        //        Assert.IsType<ClassA>(model.MapTo<ClassA>());
        //    }
        //}

        public class ConventionFacts
        {
            [Theory, AutoData]
            internal void ShouldHaveBuiltinSameNameConvention(SameName source)
            {
                Assert.Equal(source.MapTo<SameNameDto>().SomeProperty, source.SomeProperty);
            }
        }

        public class TypeConversionFacts
        {
            [Theory, AutoData]
            internal void ShouldBeAbleToConvertDateTimeToString(DateTimeClass source)
            {            
                Assert.Equal(source.MapTo<DateTimeClassDto>().Date, source.Date.ToString(CultureInfo.CurrentUICulture));
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertStringToDateTime(DateTimeClassDto source)
            {
                source.Date = DateTime.Now.ToString();
                Assert.Equal(source.MapTo<DateTimeClass>().Date, Convert.ToDateTime(source.Date));
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertIntToString(NumberClass source)
            {            
                Assert.Equal(source.MapTo<NumberClassDto>().Count, source.Count.ToString());
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertStringToInt(NumberClassDto source)
            {            
                source.Count = "199";
                Assert.Equal(source.MapTo<NumberClass>().Count, Convert.ToInt32(source.Count));
            }
        }        
    }

    public class EntityA
    {
        public string P0 { get; set; }
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
        public string P5 { get; set; }
        public string P6 { get; set; }
        public string P7 { get; set; }
        public string P8 { get; set; }
        public string P9 { get; set; }
    }

    public class EntityB
    {
        public string P0 { get; set; }
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
        public string P5 { get; set; }
        public string P6 { get; set; }
        public string P7 { get; set; }
        public string P8 { get; set; }
        public string P9 { get; set; }
    }

    public class TypeLookupModel
    {
        public string Type { get; set; }
        public string SomeValue { get; set; }
    }

    public class ClassB : ClassA
    {
        public string SomeValue { get; set; }
    }

    public class SameName
    {
        public string SomeProperty { get; set; }
    }

    public class SameNameDto
    {
        public int NotSameNameProp { get; set; }
        public string SomeProperty { get; set; }
    }

    public class NumberClass
    {
        public int Count { get; set; }
    }

    public class NumberClassDto
    {
        public string Count { get; set; }
    }

    public class DateTimeClass {
        public DateTime Date { get; set; }
    } 

    public class DateTimeClassDto {
        public string Date { get; set; }
    }

    public class ManualDto
    {        
        public string SomePropId { get; set; }     
    }

    public class Manual
    {
        public string Id { get; set; }        
    }

}