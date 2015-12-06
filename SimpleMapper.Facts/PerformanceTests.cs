using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit;
using SimpleMapper.Facts.AutoFixture;
using SimpleMapper.Facts.TestObjects;
using Xunit;
using Xunit.Extensions;

namespace SimpleMapper.Facts
{
    public class PerformanceTests
    {
        [Theory(Skip = "Performance only"), AutoTestData]
        public void ComparePropertySetterPerformance(ClassA classA)
        {
            var setValue = LambdaCompiler.CreateSetter<ClassA, string>("P1");

            const int items = 1000000;

            var timerCompiled = new Stopwatch();
            var timerReflection = new Stopwatch();

            timerCompiled.Start();

            for (var i = 0; i < items; i++)
            {
                setValue(classA, "hej");
            }

            timerCompiled.Stop();

            var info = classA.GetType().GetProperties().First(x => x.Name == "P1");

            timerReflection.Start();

            for (var i = 0; i < items; i++)
            {
                info.SetValue(classA, "hej");
            }

            timerReflection.Stop();

            Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds,
                timerReflection.ElapsedMilliseconds);
        }

        [Theory(Skip = "Performance only"), AutoTestData]
        public void ComparePropertyGetterPerformance(ClassA classA)
        {
            var getValue = LambdaCompiler.CreateGetter<ClassA, string>("P1");

            const int items = 1000000;

            var timerCompiled = new Stopwatch();
            var timerReflection = new Stopwatch();

            timerCompiled.Start();

            for (var i = 0; i < items; i++)
            {
                getValue(classA);
            }

            timerCompiled.Stop();

            var info = classA.GetType().GetProperties().First(x => x.Name == "P1");

            timerReflection.Start();

            for (var i = 0; i < items; i++)
            {
                info.GetValue(classA);
            }

            timerReflection.Stop();

            Console.WriteLine("Compiled: {0} Reflection: {1}", timerCompiled.ElapsedMilliseconds,
                timerReflection.ElapsedMilliseconds);
        }

        [Fact(Skip = "Performance only")]
        public void TestActivation()
        {
            const int items = 1000000;

            var timerCompiled = new Stopwatch();
            var timerActivator = new Stopwatch();

            var entities = new List<ClassA>();

            var classACreator = LambdaCompiler.CreateActivator<ClassA>();

            timerCompiled.Start();

            for (var i = 0; i < items; i++)
            {
                entities.Add(classACreator());
            }

            timerCompiled.Stop();

            timerActivator.Start();

            for (var i = 0; i < items; i++)
            {
                entities.Add(Activator.CreateInstance<ClassA>());
            }

            timerActivator.Stop();

            Console.WriteLine("Compiled: {0} Activator: {1}", timerCompiled.ElapsedMilliseconds,
                timerActivator.ElapsedMilliseconds);
        }

        [Theory(Skip = "Performance only"), AutoData]
        public void TestTimeMappingBasicListOfEntities(IFixture fixture)
        {
            const int ItemCount = 1000000;

            var entities = new List<EntityA>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                entities.Add(new EntityA
                {
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

            var manuallyMapped = entities.Select(e => new EntityB
            {
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
}