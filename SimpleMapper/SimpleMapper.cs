using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMapper
{
    public static class MapperExtensions
    {
        internal static ObjectMapper Mapper = new ObjectMapper();

        public static IEnumerable<TDestination> MapTo<TDestination>(this IEnumerable source) where TDestination : class
        {
            var sourceType = source.GetType();
            if (sourceType.IsGenericType && sourceType.GetGenericArguments().Length == 1)
            {
                return Mapper.MapMany<TDestination>(source, sourceType.GetGenericArguments()[0]);
            }
            return (from object item in source select item.MapTo<TDestination>());
        }

        internal static object MapToList<TDestination>(this IEnumerable source) where TDestination : class
        {
            return Mapper.MapManyAsList<TDestination>(source);
        }

        public static IEnumerable<TDestination> MapToAs<TDestination, TMapAs>(this IEnumerable source) where TDestination : class where TMapAs : class
        {
            return (from object item in source select item.MapToAs<TDestination, TMapAs>());
        }

        public static TDestination MapToAs<TDestination, TMapAs>(this object source) where TDestination : class
        {
            return Mapper.CreateDestinationObjectAndMap<TDestination>(source, typeof(TMapAs));
        }

        public static TDestination MapTo<TDestination>(this object source, TDestination to) where TDestination : class
        {
            if (source == null) return null;
            Mapper.Map(source, to);
            return to;
        }

        public static TDestination MapTo<TDestination>(this object source) where TDestination : class
        {
            return Mapper.CreateDestinationObjectAndMap<TDestination>(source, typeof(TDestination));
        }

        public static void MapFrom(this object source, params object[] items)
        {
            foreach (var item in items)
            {
                Mapper.Map(item, source);
            }
        }
    }

    public class ObjectMapper
    {
        private static IConfigurationContainer container;
        
        internal static MapperConfiguration CurrentConfiguration => VerifyConfiguration();
        
        private static MapperConfiguration VerifyConfiguration(bool initializeMappers = true, MapperConfiguration configuration = null)
        {
            if (container?.Current != null) return container.Current;

            if (container == null) container = new StaticContextContainer();

            container.Current = configuration ?? new MapperConfiguration();
            if(initializeMappers && !container.Current.IsInitialized) container.Current.Initialize();

            return container.Current;
        }

        public static void Initialize(Action<IMapperConfiguration> setup, bool initializeMappers = true, IConfigurationContainer configurationContainer = null)
        {
            var config = new MapperConfiguration();
            container = configurationContainer;
            setup(config);
            VerifyConfiguration(initializeMappers, config);
        }

        public MapperConfiguration Configuration => VerifyConfiguration();

        public IEnumerable<TDestination> MapMany<TDestination>(IEnumerable source, Type listItemType, Type lookupType = null) where TDestination : class
        {
            if (source == null) yield break;
            var map = GetMap(listItemType, lookupType ?? typeof(TDestination));

            foreach (var item in source)
            {
                var destination = CreateDestinationObject<TDestination>(item, map);
                map.Map(item, destination);
                yield return destination;
            }
        }

        public object MapManyAsList<TDestination>(IEnumerable source) where TDestination : class
        {
            var enumerable = source as object[] ?? source.Cast<object>().ToArray();
            var targetListType = typeof(TDestination);
            var sourceListType = source.GetType();

            if (!targetListType.IsGenericType || targetListType.GetGenericArguments().Length != 1)
            {
                throw new MapperException(string.Format("Could not perform list mapping for {0}, destinationtype must be generic with item type specified", targetListType.Name));
            }

            if (!sourceListType.IsGenericType || sourceListType.GetGenericArguments().Length != 1)
            {
                throw new MapperException(string.Format("Could not perform list mapping for {0}, sourcetype must be generic with item type specified", sourceListType.Name));
            }

            var sourceItemType = sourceListType.GetGenericArguments()[0];
            var destinationItemType = targetListType.GetGenericArguments()[0];

            var map = GetMap(sourceItemType, destinationItemType);
            var list = (IList)Activator.CreateInstance(typeof(TDestination).IsInterface ? typeof(List<>).MakeGenericType(destinationItemType) : typeof(TDestination), enumerable.Length);
            foreach (var item in enumerable)
            {
                var newItem = CreateDestinationObject(destinationItemType, item, map);
                map.Map(item, newItem);
                list.Add(newItem);
            }

            return list;
        }

        internal object CreateDestinationObject(Type destinationType, object source, IPropertyMap map = null)
        {
            if (map == null) map = GetMap(source.GetType(), destinationType);
            return map.CreateUsingSpecializedActivator(source) ?? Configuration.DefaultActivator(destinationType);
        }

        internal T CreateDestinationObject<T>(object source, IPropertyMap map = null) where T : class
        {
            return (T)CreateDestinationObject(typeof(T), source, map);
        }

        internal void CreateMapAndInitialize(Type source, Type destination)
        {
            var type = typeof(ConventionMap<,>).MakeGenericType(source, destination);
            var map = (IPropertyMap)(Configuration.DefaultActivator(type));
            map.Initialize();
            Configuration.Maps.Add(new KeyValuePair<Type, Type>(source, destination), map);
        }

        internal void CreateMap<TSource, TDestination>()
        {
            Configuration.Maps.Add(new KeyValuePair<Type, Type>(typeof(TSource), typeof(TDestination)), new ConventionMap<TSource, TDestination>(Configuration));
        }

        internal void Map(object source, object destination, IPropertyMap map = null)
        {
            if (source == null) return;
            if (destination == null) throw new ApplicationException("Destination object must not be null");
            if (map == null) map = GetMap(source.GetType(), destination.GetType());

            map.Map(source, destination);
        }

        internal IPropertyMap GetMap(Type sourceType, Type destinationType)
        {
            var key = new KeyValuePair<Type, Type>(sourceType, destinationType);

            if (!Configuration.IsInitialized) Configuration.Initialize();

            if (Configuration.Maps.ContainsKey(key)) return Configuration.Maps[key];

            if (Configuration.CreateMissingMapsAutomaticly) CreateMapAndInitialize(sourceType, destinationType);
            else throw new MapperException(string.Format("No map configured to map from {0} to {1}", sourceType.Name, destinationType.Name));

            return Configuration.Maps[key];
        }

        internal TDestination CreateDestinationObjectAndMap<TDestination>(object source, Type mapAs) where TDestination : class
        {
            if (source == null) return null;

            var sourceType = source.GetType();

            var resolver = Configuration.ProxyTypeResolvers?.FirstOrDefault(x => x.IsProxyObject(source));
            if (resolver != null)
            {
                sourceType = resolver.GetRealType(source);
            }

            var map = GetMap(sourceType, mapAs);
            var destination = CreateDestinationObject<TDestination>(source, map);
            Map(source, destination, map);
            return destination;
        }
    }

    public interface IPropertyMap
    {
        void Initialize();
        void Map(object source, object destination);
        object CreateUsingSpecializedActivator(object source);
    }

    public interface IPropertyConvention
    {
        IEnumerable<dynamic> Map(PropertyInfo[] source, PropertyInfo[] destination);
    }

    public class ConventionMap<TSource, TDestination> : IPropertyMap
    {
        private IEnumerable<PropertyLookup> map;
        private readonly IMapperConfiguration configuration;

        public ConventionMap() : this(ObjectMapper.CurrentConfiguration) { }

        internal ConventionMap(IMapperConfiguration configuration)
        {
            this.configuration = configuration;
            IgnoreProperties = new List<string>();
            Conventions = new List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>>();
            Conversions = new Dictionary<KeyValuePair<Type, Type>, ITypeConverter>();
        }
        
        public List<string> IgnoreProperties { get; set; }
        public Func<TSource, TDestination> CustomActivator { get; set; }
        internal Func<TDestination> SpecializedActivator { get; set; }
        public List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>> Conventions { get; set; }
        public Dictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; set; }

        public virtual void Initialize()
        {
            var conventionMap = new List<dynamic>();
            var sourceProperties = typeof(TSource).GetProperties();
            var destinationProperties = typeof(TDestination).GetProperties();
            var propertyMap = new List<PropertyLookup>();

            if (CustomActivator == null)
            {
                SpecializedActivator = LambdaCompiler.CreateActivator<TDestination>();
            }

            Conventions.Concat(configuration.Conventions).ToList().ForEach(convention => conventionMap.AddRange(convention(sourceProperties, destinationProperties)));

            conventionMap.ForEach(map =>
            {
                if (!map.source.CanRead) throw new MapperException(string.Format("Property to read from {0} has no getter!", map.source.Name));
                if (!map.destination.CanWrite) throw new MapperException(string.Format("Property to write to {0} has no setter", map.destination.Name));
                if (IgnoreProperties.Contains(map.destination.Name)) return;

                var getter = typeof(PropertyLookup.GetterInvoker<,>).MakeGenericType(typeof(TSource), map.source.PropertyType);
                var setter = typeof(PropertyLookup.SetterInvoker<,>).MakeGenericType(typeof(TDestination), map.destination.PropertyType);
                var item = new PropertyLookup
                {
                    Source = Activator.CreateInstance(getter, new object[] { map.source.Name }),
                    Destination = Activator.CreateInstance(setter, new object[] { map.destination.Name })
                };

                if (map.source.PropertyType != map.destination.PropertyType)
                {
                    item.Converter = GetConversion(new KeyValuePair<Type, Type>(map.source.PropertyType, map.destination.PropertyType));

                    if (item.Converter == null)
                    {
                        if (map.source.PropertyType.IsPrimitive && map.destination.PropertyType.IsPrimitive)
                            throw new MapperException(string.Format("Matched properties are not of same primitive type, {0}, {1}, and no conversion available!", map.source.PropertyType.Name, map.destination.PropertyType.Name));

                        if (typeof(Enum).IsAssignableFrom(map.source.PropertyType) && typeof(Enum).IsAssignableFrom(map.destination.PropertyType))
                        {
                            item.Converter = (ITypeConverter)Activator.CreateInstance(typeof(EnumConversionContainer<>).MakeGenericType(map.destination.PropertyType));
                        }
                        else
                        {
                            if (!configuration.ApplyConventionsRecursively) return;
                            item.Converter = (ITypeConverter)Activator.CreateInstance(
                                typeof(DifferentTypeConversionContainer<>).MakeGenericType(map.destination.PropertyType),
                                new object[] { typeof(TSource).Name, map.source.Name, map.destination.Name });
                        }
                    }
                }

                propertyMap.Add(item);
            });

            map = propertyMap.Distinct().ToList(); //TODO: verify distinct behavior in this case...
        }

        private ITypeConverter GetConversion(KeyValuePair<Type, Type> key)
        {
            if (Conversions.ContainsKey(key)) return Conversions[key];
            return configuration.Conversions.ContainsKey(key) ? configuration.Conversions[key] : null;
        }

        public virtual void Map(object source, object destination)
        {
            foreach (var lookup in map)
            {
                try
                {
                    var fromValue = lookup.Source.Get(source);

                    if (lookup.Converter != null)
                    {
                        fromValue = lookup.Converter.Convert(fromValue);
                    }

                    lookup.Destination.Set(destination, fromValue);
                }
                catch (Exception ex)
                {
                    throw new MapperException("There was an error setting mapped property value", ex);
                }
            }
        }

        public object CreateUsingSpecializedActivator(object source)
        {
            if (CustomActivator != null) return CustomActivator((TSource)source);
            return SpecializedActivator != null ? SpecializedActivator() : (object) null;
        }
    }

    public class ManualMap<TSource, TDestination> : ConventionMap<TSource, TDestination>
    {
        public Action<TSource, TDestination> ObjectMap { get; set; }
        public bool UseConventionMapping { get; internal set; }

        protected IPropertyMap ConventionMap { get; set; }

        public ManualMap(Action<TSource, TDestination> map, bool useConventionMapping, IMapperConfiguration configuration) : base(configuration)
        {
            ObjectMap = map;
            UseConventionMapping = useConventionMapping;
            ConventionMap = new ConventionMap<TSource, TDestination>();
        }

        public override void Initialize()
        {
            if (!UseConventionMapping) return;
            ConventionMap.Initialize();
        }

        public override void Map(object source, object destination)
        {
            try
            {
                if (UseConventionMapping)
                {
                    ConventionMap.Map(source, destination);
                }

                ObjectMap((TSource)source, (TDestination)destination);
            }
            catch (Exception ex)
            {
                throw new MapperException(string.Format("There was an error applying manual property map from {0} to {1} ", typeof(TSource).Name, typeof(TDestination).Name), ex);
            }
        }
    }

    public interface ITypeConverter
    {
        object Convert(object source);
    }

    public class CustomTypeConversionContainer<TSource, TDestination> : ITypeConverter
    {
        private readonly Func<TSource, TDestination> conversion;

        public CustomTypeConversionContainer(Func<TSource, TDestination> conversion)
        {
            this.conversion = conversion;
        }

        public object Convert(object source)
        {
            try
            {
                return conversion((TSource)source);
            }
            catch (Exception ex)
            {
                throw new MapperException(string.Format("There was an error converting source {0} to {1}.", source.GetType().Name, typeof(TDestination).Name), ex);
            }
        }
    }

    public class EnumConversionContainer<T> : ITypeConverter where T : struct
    {
        public object Convert(object source)
        {
            T destination;
            if (!Enum.TryParse(source.ToString(), out destination)) throw new MapperException(string.Format("{0} is not valid member of enumeration {1}", source, typeof(T).Name));
            return destination;
        }
    }

    public class DifferentTypeConversionContainer<TDestination> : ITypeConverter where TDestination : class
    {
        private const int MaxRecursions = 50;
        private int recursions;
        private readonly string parentClassName;
        private readonly string parentPropertyName;
        private readonly string targetPropertyName;
        public DifferentTypeConversionContainer(string parentClassName, string parentPropertyName, string targetPropertyName)
        {
            this.parentClassName = parentClassName;
            this.parentPropertyName = parentPropertyName;
            this.targetPropertyName = targetPropertyName;
        }

        public object Convert(object source)
        {
            if (recursions >= MaxRecursions) throw new MapperException(string.Format("Could not map {0} to {1} on {2} because the traversed object graph contains a circular reference. Revise your object design, map it manually or add this property to the ignore list.", parentPropertyName, targetPropertyName, parentClassName));
            recursions++;

            var enumerable = source as IEnumerable;
            var destination = enumerable != null ? enumerable.MapToList<TDestination>() : source.MapTo<TDestination>();

            recursions = 0;
            return destination;
        }
    }

    internal class PropertyLookup
    {
        public IGetter Source { get; set; }
        public ISetter Destination { get; set; }
        public ITypeConverter Converter { get; set; }

        internal interface ISetter { void Set(object item, object value); }
        internal interface IGetter { object Get(object item); }

        internal class SetterInvoker<TObject, TProperty> : ISetter
        {
            private Action<TObject, TProperty> Setter { get; set; }

            public SetterInvoker(string propertyName)
            {
                Setter = LambdaCompiler.CreateSetter<TObject, TProperty>(propertyName);
            }

            public void Set(object item, object value)
            {
                Setter((TObject)item, (TProperty)value);
            }
        }

        internal class GetterInvoker<TObject, TProperty> : IGetter
        {
            private Func<TObject, TProperty> Getter { get; set; }

            public GetterInvoker(string propertyName)
            {
                Getter = LambdaCompiler.CreateGetter<TObject, TProperty>(propertyName);
            }

            public object Get(object item)
            {
                return Getter((TObject)item);
            }
        }
    }

    internal static class LambdaCompiler
    {
        public static Func<TObject, TProperty> CreateGetter<TObject, TProperty>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(TObject), "value");
            var property = Expression.Property(parameter, propertyName);
            return Expression.Lambda<Func<TObject, TProperty>>(property, parameter).Compile();
        }

        public static Action<TObject, TProperty> CreateSetter<TObject, TProperty>(string propertyName)
        {
            var objectParameter = Expression.Parameter(typeof(TObject));
            var propertyParameter = Expression.Parameter(typeof(TProperty), propertyName);
            var property = Expression.Property(objectParameter, propertyName);
            return Expression.Lambda<Action<TObject, TProperty>>(Expression.Assign(property, propertyParameter), objectParameter, propertyParameter).Compile();
        }

        internal static Func<T> CreateActivator<T>(ConstructorInfo constructor = null)
        {
            constructor = constructor ?? typeof(T).GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
            if (constructor == null) throw new MapperException(string.Format("Cant create destination object {0}, no parameterless constructor available!", typeof(T).Name));
            return (Func<T>)Expression.Lambda(typeof(Func<T>), Expression.New(constructor)).Compile();
        }
    }

    public class MapperException : ApplicationException
    {
        public MapperException(string message) : base(message) { }
        public MapperException(string message, Exception innerException) : base(message, innerException) { }
    }

    public interface IConfigurationContainer
    {
        MapperConfiguration Current { get; set; }
    }

    public class StaticContextContainer : IConfigurationContainer
    {
        private static MapperConfiguration currentConfiguration;
        public MapperConfiguration Current { get { return currentConfiguration;} set { currentConfiguration = value; } }
    }

    public interface IProxyTypeResolver
    {
        bool IsProxyObject(object type);
        Type GetRealType(object type);
    }

    public interface IMapperConfiguration
    {
        Func<Type, object> DefaultActivator { get; set; }

        bool CreateMissingMapsAutomaticly { get; set; }
        bool ApplyConventionsRecursively { get; set; }

        IDictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; }
        IDictionary<KeyValuePair<Type, Type>, IPropertyMap> Maps { get; }
        IList<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>> Conventions { get; }
        IList<IProxyTypeResolver> ProxyTypeResolvers { get; }

        IMapperConfiguration AddMap<TSource, TDestination>(Action<TSource, TDestination> map, bool useConventionMapping) where TSource : class where TDestination : class;
        IMapperConfiguration AddMap(Type source, Type destination, IPropertyMap container);
        IMapperConfiguration AddConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>> convention);
        IMapperConfiguration AddConvention<T>() where T : IPropertyConvention;
        IMapperConfiguration AddConversion<TSource, TDestination>(Func<TSource, TDestination> conversion);

        IMapperScanner Scanner { get; set; }
    }

    //public interface IActivator
    //{
    //    object CreateInstance(Type type, params object[] args);
    //}

    public class MapperConfiguration : IMapperConfiguration
    {
        public Func<Type, object> DefaultActivator { get; set; }
        public bool CreateMissingMapsAutomaticly { get; set; }
        public bool ApplyConventionsRecursively { get; set; }
        public IDictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; }
        public IList<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>> Conventions { get; }
        public IDictionary<KeyValuePair<Type, Type>, IPropertyMap> Maps { get; }

        public IMapperScanner Scanner { get; set; }

        public MapperConfiguration()
        {
            ProxyTypeResolvers = new List<IProxyTypeResolver>();
            Maps = new Dictionary<KeyValuePair<Type, Type>, IPropertyMap>();
            Scanner = new MapperScanner();
            Conventions = new List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>>>();
            Conversions = new Dictionary<KeyValuePair<Type, Type>, ITypeConverter>();
            CreateMissingMapsAutomaticly = true;
            ApplyConventionsRecursively = true;
            DefaultActivator = Activator.CreateInstance;

            new DefaultConfiguration().Configure(this);
        }

        internal bool IsInitialized { get; set; }

        internal IMapperConfiguration Initialize()
        {
            Maps.Clear();

            var mapperTypes = Scanner.ScanForMappers();

            if(DefaultActivator == null) throw new MapperException("No default activator configured, cant create mappers");

            foreach (var type in mapperTypes.Where(x => typeof(Mapper).IsAssignableFrom(x)))
            {
                try
                {
                    DefaultActivator(type);
                }
                catch (Exception ex)
                {
                    throw new MapperException("There was an error creating mapper " + type.Name, ex);
                }
            }

            foreach (var map in Maps.Values)
            {
                map.Initialize();
            }

            IsInitialized = true;

            return this;
        }

        public IMapperConfiguration AddMap<TSource, TDestination>(Action<TSource, TDestination> map, bool useConventionMapping) where TSource : class where TDestination : class
        {
            AddMap(typeof(TSource), typeof(TDestination), new ManualMap<TSource, TDestination>(map, useConventionMapping, this));
            return this;
        }

        public IMapperConfiguration AddMap(Type source, Type destination, IPropertyMap container)
        {
            Maps.Add(new KeyValuePair<Type, Type>(source, destination), container);
            return this;
        }

        public IMapperConfiguration AddConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention)
        {
            Conventions.Add(convention);
            return this;
        }

        public IMapperConfiguration AddConvention<T>() where T : IPropertyConvention
        {
            try
            {
                var convention = Activator.CreateInstance<T>();
                AddConvention(convention.Map);
            }
            catch (Exception ex)
            {
                throw new MapperException("There was an error creating custom convention", ex);
            }
            return this;
        }

        public IMapperConfiguration AddConversion<TSource, TDestination>(Func<TSource, TDestination> conversion)
        {
            Conversions[new KeyValuePair<Type, Type>(typeof(TSource), typeof(TDestination))] = new CustomTypeConversionContainer<TSource, TDestination>(conversion);
            return this;
        }

        public IList<IProxyTypeResolver> ProxyTypeResolvers { get; protected set; }
    }
    
    public interface IMapperScanner
    {
        IMapperScanner ScanAssembliesContainingType<T>();
        bool ScanAllAssembliesInBaseFolder { get; set; }
        IEnumerable<Type> ScanForMappers();
    }

    internal class MapperScanner : IMapperScanner
    {
        private List<Assembly> Assemblies { get; }  = new List<Assembly>();
        public bool ScanAllAssembliesInBaseFolder { get; set; } = true;

        public IMapperScanner ScanAssembliesContainingType<T>() {
            Assemblies.Add(typeof (T).Assembly);
            return this;
        }

        public IEnumerable<Type> ScanForMappers()
        {
            if (ScanAllAssembliesInBaseFolder)
            {
                var baseUri = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/");
                Assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(x =>
                {
                    try { return x.CodeBase.Contains(baseUri); }
                    catch
                    { 
                        // ignored 
                    }
                    return false;
                }).ToList());    
            }

            return
                from assembly in Assemblies
                from types in assembly.GetTypes()
                where typeof(Mapper).IsAssignableFrom(types) && !types.IsAbstract
                select types;
        }
    }

    public abstract class Mapper
    {
        private readonly IMapperConfiguration configuration;

        protected Mapper()
        {
            configuration = ObjectMapper.CurrentConfiguration;
            Map = new SetupMapping(configuration);
            Configure = new SetupConfiguration(configuration);
        }

        protected Mapper(IMapperConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public SetupConfiguration Configure { get; protected set; }

        public class SetupConfiguration
        {
            protected readonly IMapperConfiguration Configuration;

            public SetupConfiguration(IMapperConfiguration configuration)
            {
                Configuration = configuration;
            }
            
            public SetupConfiguration WithConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention)
            {
                Configuration.Conventions.Add(convention);
                return this;
            }

            public SetupConfiguration WithConvention<T>() where T : IPropertyConvention
            {
                Configuration.AddConvention<T>();
                return this;
            }

            public SetupConfiguration WithConversion<TSource, TDestination>(Func<TSource, TDestination> conversion){
                Configuration.AddConversion(conversion);
                return this;
            }
        }

        public SetupMapping Map { get; protected set; }

        protected void ConvertUsing<TFrom, TTo>(Func<TFrom, TTo> conversion)
        {
            configuration.AddConversion(conversion);
        }

        public class SetupMapping
        {
            protected readonly IMapperConfiguration Configuration;

            public SetupMapping(IMapperConfiguration configuration)
            {
                Configuration = configuration;
            }

            public SetupMap<TSource, TDestination> FromTo<TSource, TDestination>() where TSource : class where TDestination : class
            {
                return new MapTo<TSource>(Configuration).To<TDestination>();
            }

            public MapTo<TSource> From<TSource>() where TSource : class
            {
                return new MapTo<TSource>(Configuration);
            }

            public void WithConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention)
            {
                Configuration.AddConvention(convention);
            }

            public void WithConvention<T>() where T : IPropertyConvention
            {
                try
                {
                    var convention = Activator.CreateInstance<T>();
                    Configuration.AddConvention(convention.Map);
                }
                catch (Exception ex)
                {
                    throw new MapperException("There was an error creating custom convention", ex);
                }
            }

            public class SetupConventionsOnManual<TSource, TDestination> where TSource : class where TDestination : class
            {
                private readonly ManualMap<TSource, TDestination> _map;

                public SetupConventionsOnManual(ManualMap<TSource, TDestination> map)
                {
                    _map = map;
                }

                public void IgnoreConventions()
                {
                    _map.UseConventionMapping = false;
                }
            }

            public class MapTo<TSource> where TSource : class
            {
                internal readonly IMapperConfiguration Configuration;

                public MapTo(IMapperConfiguration configuration)
                {
                    Configuration = configuration;
                }

                public SetupMap<TSource, TDestination> To<TDestination>() where TDestination : class
                {
                    var manualMap = new ManualMap<TSource, TDestination>(null, true, Configuration);
                    Configuration.AddMap(typeof(TSource), typeof(TDestination), manualMap);
                    return new SetupMap<TSource, TDestination>(manualMap, Configuration);
                }
            }

            public class SetupMap<TSource, TDestination> where TSource : class where TDestination : class
            {
                internal readonly IMapperConfiguration Configuration;
                private readonly ManualMap<TSource, TDestination> map;

                public SetupMap(ManualMap<TSource, TDestination> map, IMapperConfiguration configuration)
                {
                    this.map = map;
                    Configuration = configuration;
                }

                public SetupMap<TSource, TDestination> IncludeFrom<T>()
                {
                    Debug.Assert(typeof(TSource).IsAssignableFrom(typeof(T)), "TSource must be assignable from T in order to share a property map");
                    Configuration.AddMap(typeof(T), typeof(TDestination), map);
                    return this;
                }

                public SetupMap<TSource, TDestination> IncludeTo<T>()
                {
                    Debug.Assert(typeof(TDestination).IsAssignableFrom(typeof(T)), "TDestination must be assignable from T in order to share a property map");
                    Configuration.AddMap(typeof(T), typeof(TDestination), map);
                    return this;
                }

                public SetupMap<TSource, TDestination> WithCustomConvention(
                    Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention)
                {
                    map.Conventions.Add(convention);
                    return this;
                }

                internal SetupMap<TSource, TDestination> WithCustomConvention<T>() where T : IPropertyConvention
                {
                    try{
                        var convention = Activator.CreateInstance<T>();
                        map.Conventions.Add(convention.Map);
                    }
                    catch (Exception ex)
                    {
                        throw new MapperException("There was an error creating custom convention", ex);
                    }
                    
                    return this;
                }

                public SetupMap<TSource, TDestination> WithCustomConversion<TFrom, TTo>(Func<TFrom, TTo> conversion)
                {
                    map.Conversions.Add(new KeyValuePair<Type, Type>(typeof(TFrom), typeof(TTo)), new CustomTypeConversionContainer<TFrom, TTo>(conversion));
                    return this;
                }

                public SetupConventionsOnManual<TSource, TDestination> SetManually(Action<TSource, TDestination> map)
                {
                    this.map.ObjectMap = map;
                    return new SetupConventionsOnManual<TSource, TDestination>(this.map);
                }

                public SetupMap<TSource, TDestination> CreateWith(Func<TSource, TDestination> activator)
                {
                    map.CustomActivator = activator;
                    return this;
                }

                public SetupMap<TSource, TDestination> Set(params Expression<Func<TDestination, object>>[] properties)
                {
                    var ignoreList = typeof(TDestination).GetProperties().Select(x => x.Name).ToList();
                    var selectedProperties = properties.Select(GetPropertyNameFromLambda);
                    ignoreList.RemoveAll(selectedProperties.Contains);
                    map.IgnoreProperties.AddRange(ignoreList);
                    return this;
                }

                public SetupMap<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] properties)
                {
                    map.IgnoreProperties.AddRange(properties.Select(GetPropertyNameFromLambda));
                    return this;
                }

                internal static string GetPropertyNameFromLambda(Expression<Func<TDestination, object>> expression)
                {
                    var lambda = expression as LambdaExpression;
                    Debug.Assert(lambda != null, "Not a valid lambda epression");
                    MemberExpression memberExpression;

                    if (lambda.Body is UnaryExpression)
                    {
                        var unaryExpression = lambda.Body as UnaryExpression;
                        memberExpression = unaryExpression.Operand as MemberExpression;
                    }
                    else
                    {
                        memberExpression = lambda.Body as MemberExpression;
                    }

                    Debug.Assert(memberExpression != null, "Please provide a lambda expression like 'x => x.PropertyName'");
                    var propertyInfo = (PropertyInfo)memberExpression.Member;

                    return propertyInfo.Name;
                }
            }
        }
    }

    public class SameNameIgnoreCaseConvention : IPropertyConvention
    {
        public IEnumerable<dynamic> Map(PropertyInfo[] s, PropertyInfo[] d)
        {
            return from destination in d
                join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                where source.CanRead && destination.CanWrite
                select new{source, destination};
        }
    }

    public class DefaultConfiguration
    {
        public static readonly Func<DateTime, string> DateToStringConversion = time => time.ToString(CultureInfo.CurrentUICulture);
        public static readonly Func<string, DateTime> StringToDateConversion = s => DateTime.Parse(s);
        public static readonly Func<int, string> IntToStringConversion = i => i.ToString(CultureInfo.CurrentCulture);
        public static readonly Func<string, int> StringToIntConversion = s => int.Parse(s);
        public static readonly Func<bool, string> BoolToStringConversion = b => b.ToString();
        public static readonly Func<string, bool> StringToBoolConversion = s => bool.Parse(s);

        public void Configure(MapperConfiguration configuration) {
            configuration
                .AddConvention<SameNameIgnoreCaseConvention>()
                .AddConversion(DateToStringConversion)
                .AddConversion(StringToDateConversion)
                .AddConversion(IntToStringConversion)
                .AddConversion(StringToIntConversion)
                .AddConversion(BoolToStringConversion)
                .AddConversion(StringToBoolConversion);
        }
    }
}