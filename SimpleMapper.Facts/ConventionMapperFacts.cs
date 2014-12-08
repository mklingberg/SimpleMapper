using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts{
    public class ConventionMapperFacts{
        public class PerformanceTests{
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
                var setValue = LambdaCompiler.CreateSetter<ClassA, string>("P1");

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

                Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds,
                    timerReflection.ElapsedMilliseconds);
            }

            [Theory, AutoTestData]
            public void ComparePropertyGetterPerformance(ClassA classA){
                var getValue = LambdaCompiler.CreateGetter<ClassA, string>("P1");

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

                Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds,
                    timerReflection.ElapsedMilliseconds);
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

                Console.WriteLine("Compiled: {0} Activator: {1}", timerCompiled.ElapsedMilliseconds,
                    timerActivator.ElapsedMilliseconds);
            }

            [Theory, AutoData]
            public void TestTimeMappingBasicListOfEntities(IFixture fixture){
                const int ItemCount = 1000000;

                var entities = new List<EntityA>(ItemCount);

                for (var i = 0; i < ItemCount; i++){
                    entities.Add(new EntityA{
                                                P0 = "1",
                                                P1 = "2",
                                                P2 = "2",
                                                P3 = "3",
                                                P4 = "4",
                                                P5 = "5",
                                                P6 = "6",
                                                P7 = "7",
                                                P8 = "8",
                                                P9 = "9"
                                            });
                }

                var mapper = new ObjectMapper();

                mapper.CreateMap<EntityA, EntityB>();
                mapper.Configuration.Initialize();

                var timerSimpleMapper = new Stopwatch();
                var timerManual = new Stopwatch();
                var timerAutoMapper = new Stopwatch();

                timerSimpleMapper.Start();

                var autoMappedItems = entities.MapTo<EntityB>().ToList();

                timerSimpleMapper.Stop();

                timerManual.Start();

                var manuallyMapped = entities.Select(e => new EntityB{
                                                                         P0 = e.P0,
                                                                         P1 = e.P1,
                                                                         P2 = e.P2,
                                                                         P3 = e.P3,
                                                                         P4 = e.P4,
                                                                         P5 = e.P5,
                                                                         P6 = e.P6,
                                                                         P7 = e.P7,
                                                                         P8 = e.P8,
                                                                         P9 = e.P9
                                                                     }).ToList();
                timerManual.Stop();

                AutoMapper.Mapper.CreateMap<EntityA, EntityB>();

                timerAutoMapper.Start();

                var result = AutoMapper.Mapper.Map<IEnumerable<EntityB>>(entities);

                timerSimpleMapper.Stop();

                Console.WriteLine("Mapped {3} entities, SimpleMapper: {0}ms TimeManual: {1}ms AutoMapper: {2}ms",
                    timerSimpleMapper.ElapsedMilliseconds, timerManual.ElapsedMilliseconds,
                    timerAutoMapper.ElapsedMilliseconds, ItemCount);
            }
        }

        public class ConventionFacts{
            [Theory, AutoData]
            internal void ShouldHaveBuiltinSameNameConvention(SameName source){
                Assert.Equal(source.MapTo<SameNameDto>().SomeProperty, source.SomeProperty);
            }
        }

        public class ClassTests{
            [Theory, AutoTestData]
            public void Test(TestClass1 testClass1){
                var model = testClass1.MapTo<TestClass1Model>();
            }

            public class TestClass1{
                public TestClass2 Class2 { get; set; }
            }

            public class TestClass1Model{
                public TestClass2Model Class2 { get; set; }
            }

            public class TestClass2{
                public string SomeProp { get; set; }
            }

            public class TestClass2Model{
                public string SomeProp { get; set; }
            }
        }

        public class TypeConversionFacts{
            [Theory, AutoData]
            internal void ShouldBeAbleToConvertDateTimeToString(DateTimeClass source){
                Assert.Equal(source.MapTo<DateTimeClassDto>().Date, source.Date.ToString(CultureInfo.CurrentUICulture));
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertStringToDateTime(DateTimeClassDto source){
                source.Date = DateTime.Now.ToString();
                Assert.Equal(source.MapTo<DateTimeClass>().Date, Convert.ToDateTime(source.Date));
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertIntToString(NumberClass source){
                Assert.Equal(source.MapTo<NumberClassDto>().Count, source.Count.ToString());
            }

            [Theory, AutoData]
            internal void ShouldBeAbleToConvertStringToInt(NumberClassDto source){
                source.Count = "199";
                Assert.Equal(source.MapTo<NumberClass>().Count, Convert.ToInt32(source.Count));
            }

            [Theory, AutoTestData]
            internal void ShouldBeAbleToConvertEnumsWithSameValue(EnumEntityA entityA){
                entityA.Enum1 = Enum1.B;
                var entityB = entityA.MapTo<EnumEntityB>();
                Assert.True(entityB.Enum1.ToString() == entityA.Enum1.ToString());
            }

            [Theory, AutoTestData]
            internal void ShouldThrowExceptionWhenConvertingToEnumThatLacksSourceValue(EnumEntityB entityA){
                entityA.Enum1 = Enum2.E;
                Assert.Throws<MapperException>(() => entityA.MapTo<EnumEntityA>());
            }

            [Theory, AutoTestData]
            internal void ShouldBePossibleToConvertBoolToString(BoolClassA boolClass){
                Assert.True(boolClass.MapTo<BoolClassAModel>().Flag == boolClass.Flag.ToString());
            }

            [Theory, AutoTestData]
            internal void ShouldBePossibleToConvertStringToBool(BoolClassAModel model){
                model.Flag = "true";
                Assert.True(model.MapTo<BoolClassA>().Flag);
            }

            [Theory, AutoTestData]
            internal void ShouldThrowExceptionWhenDetectingInfiniteLoops(){
                var parent = new ParentClass{ChildObject = new ChildClass()};
                parent.ChildObject.ParentObject = parent;

                Assert.Throws<MapperException>(() => parent.MapTo<ParentClassModel>());
            }

            [Theory, AutoTestData]
            internal void ShouldMapChildObjectsAndListsRecursively(RecursiveClass1 source){
                var model = source.MapTo<RecursiveClass1Model>();

                Assert.True(model.ClassList.Count == source.ClassList.Count);
                Assert.True(model.OtherClass.DataValue == source.OtherClass.DataValue);
            }
        }
    }

    public class RecursiveClass1{
        public string Data { get; set; }
        public RecursiveClass2 OtherClass { get; set; }
        public List<RecursiveClass2> ClassList { get; set; }
    }

    public class RecursiveClass1Model{
        public string Data { get; set; }
        public RecursiveClass2Model OtherClass { get; set; }
        public List<RecursiveClass2Model> ClassList { get; set; }
    }

    public class RecursiveClass2{
        public string DataValue { get; set; }
    }

    public class RecursiveClass2Model{
        public string DataValue { get; set; }
    }

    public class BoolClassA{
        public bool Flag { get; set; }
    }

    public class BoolClassAModel{
        public string Flag { get; set; }
    }

    public class ParentClass{
        public ChildClass ChildObject { get; set; }
    }

    public class ParentClassModel{
        public ChildClassModel ChildObject { get; set; }
    }

    public class ChildClass{
        public ParentClass ParentObject { get; set; }
    }

    public class ChildClassModel{
        public ParentClassModel ParentObject { get; set; }
    }

    public class EnumEntityA{
        public Enum1 Enum1 { get; set; }
    }

    public class EnumEntityB{
        public Enum2 Enum1 { get; set; }
    }

    public enum Enum1{
        A,
        B,
        C
    }

    public enum Enum2{
        A,
        B,
        C,
        D,
        E
    }


    public class EntityA{
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

    public class EntityB{
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

    public class TypeLookupModel{
        public string Type { get; set; }
        public string SomeValue { get; set; }
    }

    public class ClassB : ClassA{
        public string SomeValue { get; set; }
    }

    public class SameName{
        public string SomeProperty { get; set; }
    }

    public class SameNameDto{
        public int NotSameNameProp { get; set; }
        public string SomeProperty { get; set; }
    }

    public class NumberClass{
        public int Count { get; set; }
    }

    public class NumberClassDto{
        public string Count { get; set; }
    }

    public class DateTimeClass{
        public DateTime Date { get; set; }
    }

    public class DateTimeClassDto{
        public string Date { get; set; }
    }

    public class ManualDto{
        public string SomePropId { get; set; }
    }

    public class Manual{
        public string Id { get; set; }
    }
}