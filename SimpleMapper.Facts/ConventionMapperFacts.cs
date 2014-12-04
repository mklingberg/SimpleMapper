using System;
using System.Collections.Generic;
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
        public class PerformanceTests
        {
            [Theory, AutoTestData]
            public void ShouldBePossibleToGetPropertyValue(ClassA classA){
                var getter = LambdaCompiler.CreateGetter<ClassA, string>("P1");
                Assert.True(classA.P1 == getter(classA));
            }

            [Theory, AutoTestData]
            public void ShouldBePossibleToSetPropertyValue(ClassA classA){
                var setter = LambdaCompiler.CreateSetter<ClassA, string>("P1");
                setter(classA, "hej");
                Assert.True(classA.P1 == "hej");
            }

            [Theory, AutoTestData]
            public void ComparePropertySetterPerformance(ClassA classA){
                var setValue = LambdaCompiler.CreateSetter<ClassA, string> ("P1");
                
                const int items = 1000000;

                var timerCompiled = new Stopwatch();
                var timerReflection = new Stopwatch();
                
                timerCompiled.Start();
                
                for (var i = 0; i < items; i++){
                    setValue(classA, "hej");
                }

                timerCompiled.Stop();

                var info = classA.GetType().GetProperties().First(x => x.Name == "P1");

                timerReflection.Start();

                for (var i = 0; i < items; i++){
                    info.SetValue(classA, "hej");
                }

                timerReflection.Stop();

                Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds, timerReflection.ElapsedMilliseconds);
            }

            [Theory, AutoTestData]
            public void ComparePropertyGetterPerformance(ClassA classA){
                var getValue = LambdaCompiler.CreateGetter<ClassA, string> ("P1");
                
                const int items = 1000000;

                var timerCompiled = new Stopwatch();
                var timerReflection = new Stopwatch();
                
                timerCompiled.Start();
                
                for (var i = 0; i < items; i++){
                    getValue(classA);
                }

                timerCompiled.Stop();

                var info = classA.GetType().GetProperties().First(x => x.Name == "P1");

                timerReflection.Start();

                for (var i = 0; i < items; i++){
                    info.GetValue(classA);
                }

                timerReflection.Stop();

                Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds, timerReflection.ElapsedMilliseconds);
            }

            [Fact]
            public void TestActivation(){

                const int items = 1000000;

                var timerCompiled = new Stopwatch();
                var timerActivator = new Stopwatch();

                var entities = new List<ClassA>();

                var classACreator = LambdaCompiler.CreateActivator<ClassA>();

                timerCompiled.Start();

                for (var i = 0; i < items; i++){
                    entities.Add(classACreator());    
                }

                timerCompiled.Stop();

                timerActivator.Start();

                for (var i = 0; i < items; i++){
                    entities.Add(Activator.CreateInstance<ClassA>());    
                }

                timerActivator.Stop();

                Console.WriteLine("Compiled: {0} Activator: {1}", timerCompiled.ElapsedMilliseconds, timerActivator.ElapsedMilliseconds);
                
            }

            [Theory, AutoData]
            public void TestTimeMappingEntititesUsingConventionMapperAndCompareToManualMapping(IFixture fixture){
                const int ItemCount = 1000000;

                var entities = new List<EntityA>(ItemCount);

                for (var i = 0; i < ItemCount; i++){
                    entities.Add(new EntityA { P0 = "1", P1 = "2", P2 = "2", P3 = "3", P4 = "4", P5 = "5", P6 = "6", P7 = "7", P8 = "8", P9 = "9"});
                }

                var mapper = new ObjectMapper();

                mapper.CreateMap<EntityA, EntityB>();
                mapper.Configuration.Initialize();

                var timerAuto = new Stopwatch();
                var timerManual = new Stopwatch();

                timerAuto.Start();

                var autoMappedItems = entities.MapTo<EntityB>().ToList();

                timerAuto.Stop();

                timerManual.Start();

                var manuallyMapped = entities.Select(e => new EntityB {
                                                           P0 = e.P0, P1 = e.P1, P2 = e.P2, P3 = e.P3, P4 = e.P4, P5 = e.P5, P6 = e.P6, P7 = e.P7, P8 = e.P8, P9 = e.P9
                                                       }).ToList();
                timerManual.Stop();

                Console.WriteLine("TimeMapper: {0} TimeManual: {1}", timerAuto.ElapsedMilliseconds, timerManual.ElapsedMilliseconds);
            }
        }
       
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