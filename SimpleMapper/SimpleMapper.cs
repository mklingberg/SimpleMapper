using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMapper{
    public static class MapperExtensions{
        internal static ObjectMapper Mapper = new ObjectMapper();

        public static IEnumerable<TDestination> MapTo<TDestination>(this IEnumerable source) where TDestination : class{
            var sourceType = source.GetType();
            if (sourceType.IsGenericType && sourceType.GetGenericArguments().Length == 1){
                return Mapper.MapMany<TDestination>(source, sourceType.GetGenericArguments()[0]);
            }
            return (from object item in source select item.MapTo<TDestination>());
        }

        public static IEnumerable<TDestination> MapToAs<TDestination, TMapAs>(this IEnumerable source)
            where TDestination : class where TMapAs : class{
            return (from object item in source select item.MapToAs<TDestination, TMapAs>());
        }

        public static TDestination MapToAs<TDestination, TMapAs>(this object source) where TDestination : class{
            return Mapper.CreateDestinationObjectAndMap<TDestination>(source, typeof (TMapAs));
        }

        public static TDestination MapTo<TDestination>(this object source, TDestination to) where TDestination : class{
            if (source == null) return null;
            Mapper.Map(source, to);
            return to;
        }

        public static TDestination MapTo<TDestination>(this object source) where TDestination : class{
            return Mapper.CreateDestinationObjectAndMap<TDestination>(source, typeof (TDestination));
        }

        public static void MapFrom(this object source, params object[] items){
            foreach (var item in items){
                Mapper.Map(item, source);
            }
        }
    }

    public interface IMapperConfiguration{
        Func<Type, object> CustomActivator { get; }
        bool CreateMissingMapsAutomaticly { get; }
        IDictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; }
        IList<Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>>> Conventions { get; }
        IDictionary<KeyValuePair<Type, Type>, IPropertyMap> Maps { get; }

        void AddMap<TSource, TDestination>(Action<TSource, TDestination> map, bool useConventionMapping)
            where TSource : class where TDestination : class;

        void AddMap(Type source, Type destination, IPropertyMap container);
        void AddConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<object>> convention);
        void AddConversion<TSource, TDestination>(Func<TSource, TDestination> conversion);
        IMapperConfiguration Initialize();
    }

    public class MapperConfiguration : IMapperConfiguration{
        public Func<Type, object> CustomActivator { get; set; }
        public bool CreateMissingMapsAutomaticly { get; set; }
        public IDictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; private set; }
        public IList<Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>>> Conventions { get; private set; }
        public IDictionary<KeyValuePair<Type, Type>, IPropertyMap> Maps { get; private set; }

        internal MapperConfigLoader ConfigLoader { get; private set; }

        public MapperConfiguration(){
            Maps = new Dictionary<KeyValuePair<Type, Type>, IPropertyMap>();
            ConfigLoader = new MapperConfigLoader();
            Conventions = new List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>>>{ ObjectMapper.SameNameConventionIgnoreCase };
            Conversions = new Dictionary<KeyValuePair<Type, Type>, ITypeConverter>();
            AddConversion(ObjectMapper.DateToStringConversion);
            AddConversion(ObjectMapper.StringToDateConversion);
            AddConversion(ObjectMapper.IntToStringConversion);
            AddConversion(ObjectMapper.StringToIntConversion);
            CreateMissingMapsAutomaticly = true;
            CustomActivator = null;
        }

        public IMapperConfiguration Initialize(){
            ConfigLoader.ActivateMappers();
            foreach (var map in Maps.Values){
                map.Initialize();
            }
            return this;
        }

        public void AddMap<TSource, TDestination>(Action<TSource, TDestination> map, bool useConventionMapping)
            where TSource : class where TDestination : class{
            AddMap(typeof (TSource), typeof (TDestination), new ManualMap<TSource, TDestination>(map, useConventionMapping, this));
        }

        public void AddMap(Type source, Type destination, IPropertyMap container){
            Maps.Add(new KeyValuePair<Type, Type>(source, destination), container);
        }

        public void AddConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention){
            Conventions.Add(convention);
        }

        public void AddConversion<TSource, TDestination>(Func<TSource, TDestination> conversion){
            Conversions[new KeyValuePair<Type, Type>(typeof (TSource), typeof (TDestination))] = new TypeConversionContainer<TSource, TDestination>(conversion);
        }
    }

    public class ObjectMapper{
        private static IMapperConfiguration _currentConfiguration;

        internal static IMapperConfiguration CurrentConfiguration{
            get { return _currentConfiguration ?? (_currentConfiguration = new MapperConfiguration().Initialize()); }
        }

        public static void Configure(IMapperConfiguration configuration){
            _currentConfiguration = configuration.Initialize();
        }

        public IMapperConfiguration Configuration { get; set; }

        public static Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> SameNameConventionIgnoreCase =
            (s, d) => from destination in d
                join source in s on destination.Name.ToLower() equals source.Name.ToLower()
                where source.CanRead && destination.CanWrite
                select new{source, destination};

        public static readonly Func<DateTime, string> DateToStringConversion =
            time => time.ToString(CultureInfo.CurrentUICulture);

        public static readonly Func<string, DateTime> StringToDateConversion = s => DateTime.Parse(s);
        public static readonly Func<int, string> IntToStringConversion = i => i.ToString(CultureInfo.CurrentCulture);
        public static readonly Func<string, int> StringToIntConversion = s => Int32.Parse(s);

        internal ObjectMapper(){
            Configuration = CurrentConfiguration;
        }

        internal ObjectMapper(IMapperConfiguration configuration){
            Configuration = configuration;
        }

        public IEnumerable<TDestination> MapMany<TDestination>(IEnumerable source, Type listItemType, Type lookupType = null) where TDestination : class{
            if (source == null) return new TDestination[0];
            var enumerable = source as object[] ?? source.Cast<object>().ToArray();
            var items = new TDestination[enumerable.Length];
            var map = GetMap(listItemType, lookupType ?? typeof (TDestination));

            for (var i = 0; i < enumerable.Length; i++){
                var item = enumerable[i];
                var destination = CreateDestinationObject<TDestination>(item, map);
                map.Map(item, destination);
                items[i] = destination;
            }
            return items;
        }

        internal T CreateDestinationObject<T>(object source, IPropertyMap map = null) where T : class{
            if (map == null) map = GetMap(source.GetType(), typeof (T));

            var destination = map.CreateDestinationObject(source);

            return (T) destination ?? (T) Configuration.CustomActivator(typeof (T));
        }

        internal void CreateMapAndInitialize(Type source, Type destination){
            var type = typeof (ConventionMap<,>).MakeGenericType(source, destination);
            var map = (IPropertyMap) (Configuration.CustomActivator != null ? Configuration.CustomActivator(type) : Activator.CreateInstance(type));
            map.Initialize();
            Configuration.Maps.Add(new KeyValuePair<Type, Type>(source, destination), map);
        }

        internal void CreateMap<TSource, TDestination>(){
            Configuration.Maps.Add(new KeyValuePair<Type, Type>(typeof (TSource), typeof (TDestination)), new ConventionMap<TSource, TDestination>(Configuration));
        }

        internal void Map(object source, object destination, IPropertyMap map = null){
            if (source == null) return;
            if (destination == null) throw new ApplicationException("Destination object must not be null");
            if (map == null) map = GetMap(source.GetType(), destination.GetType());

            map.Map(source, destination);
        }

        internal IPropertyMap GetMap(Type sourceType, Type destinationType){
            var key = new KeyValuePair<Type, Type>(sourceType, destinationType);

            if (Configuration.Maps.ContainsKey(key)) return Configuration.Maps[key];

            if (Configuration.CreateMissingMapsAutomaticly) CreateMapAndInitialize(sourceType, destinationType);
            else throw new MapperException(string.Format("No map configured to map from {0} to {1}", sourceType.Name, destinationType.Name));

            return Configuration.Maps[key];
        }

        internal TDestination CreateDestinationObjectAndMap<TDestination>(object source, Type mapAs)
            where TDestination : class{
            if (source == null) return null;
            var map = GetMap(source.GetType(), mapAs);
            var destination = CreateDestinationObject<TDestination>(source, map);
            Map(source, destination, map);
            return destination;
        }
    }

    public interface IPropertyMap{
        void Map(object source, object destination);
        object CreateDestinationObject(object source);
        void Initialize();
    }

    public class ConventionMap<TSource, TDestination> : IPropertyMap{
        private IEnumerable<PropertyLookup> _map;
        private readonly IMapperConfiguration _configuration;

        public ConventionMap() : this(ObjectMapper.CurrentConfiguration){}
        public ConventionMap(IMapperConfiguration configuration){
            _configuration = configuration;
            IgnoreProperties = new List<string>();
            Conventions = new List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>>>();
            Conversions = new Dictionary<KeyValuePair<Type, Type>, ITypeConverter>();
        }

        public List<string> IgnoreProperties { get; set; }
        public Func<TSource, TDestination> CustomActivator { get; set; }
        internal Func<TDestination> SpecializedActivator { get; set; }
        public List<Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>>> Conventions { get; set; }
        public Dictionary<KeyValuePair<Type, Type>, ITypeConverter> Conversions { get; set; }
        
        public virtual void Initialize(){
            var conventionMap = new List<dynamic>();
            var sourceProperties = typeof (TSource).GetProperties();
            var destinationProperties = typeof (TDestination).GetProperties();
            var propertyMap = new List<PropertyLookup>();

            if (CustomActivator == null && _configuration.CustomActivator == null){ 
                SpecializedActivator = LambdaCompiler.CreateActivator<TDestination>(); 
            }

            Conventions.Concat(_configuration.Conventions).ToList().ForEach(convention => conventionMap.AddRange(convention(sourceProperties, destinationProperties)));

            conventionMap.ForEach(map =>{
                if (!map.source.CanRead) throw new MapperException(string.Format("Property to read from {0} has no getter!", map.source.Name));
                if (!map.destination.CanWrite) throw new MapperException(string.Format("Property to write to {0} has no setter", map.destination.Name));
                if (IgnoreProperties.Contains(map.destination.Name)) return;

                var getter = typeof(PropertyLookup.GetterInvoker<,>).MakeGenericType(typeof(TSource), map.source.PropertyType);
                var setter = typeof(PropertyLookup.SetterInvoker<,>).MakeGenericType(typeof(TDestination), map.destination.PropertyType);
                var item = new PropertyLookup{Source = Activator.CreateInstance(getter, new object[]{map.source.Name}), Destination = Activator.CreateInstance(setter, new object[] {map.destination.Name})};

                if (map.source.PropertyType.Name != map.destination.PropertyType.Name){
                    item.Converter = GetConversion(new KeyValuePair<Type, Type>(map.source.PropertyType, map.destination.PropertyType));
                }

                propertyMap.Add(item);
            });

            _map = propertyMap.Distinct().ToList(); //TODO: verify distinct behavior in this case...
        }

        private ITypeConverter GetConversion(KeyValuePair<Type, Type> key){
            if (Conversions.ContainsKey(key)) return Conversions[key];
            if (_configuration.Conversions.ContainsKey(key)) return _configuration.Conversions[key];

            throw new MapperException("Matched properties are not of same type, and no conversion available!");
        }

        public virtual void Map(object source, object destination){
            foreach (var lookup in _map){
                try{
                    var fromValue = lookup.Source.Get(source);

                    if (lookup.Converter != null){
                        fromValue = lookup.Converter.Convert(fromValue);
                    }

                    lookup.Destination.Set(destination, fromValue);
                }
                catch (Exception ex){
                    throw new MapperException("There was an error setting mapped property value", ex);
                }
            }
        }

        public object CreateDestinationObject(object source){
            if (CustomActivator != null) return CustomActivator((TSource) source);
            return SpecializedActivator != null ? SpecializedActivator() : _configuration.CustomActivator(typeof (TDestination));
        }
    }

    public class ManualMap<TSource, TDestination> : ConventionMap<TSource, TDestination>{
        public Action<TSource, TDestination> ObjectMap { get; set; }
        public bool UseConventionMapping { get; internal set; }

        public ManualMap(Action<TSource, TDestination> map, bool useConventionMapping, IMapperConfiguration configuration) : base(configuration){            
            ObjectMap = map;
            UseConventionMapping = useConventionMapping;
        }

        public override void Initialize(){
            if (!UseConventionMapping) return;
            base.Initialize();
        }

        public override void Map(object source, object destination){
            try{
                if (UseConventionMapping){
                    base.Map(source, destination);
                }

                ObjectMap((TSource) source, (TDestination) destination);
            }
            catch (Exception ex){
                throw new MapperException( string.Format("There was an error applying manual property map from {0} to {1} ", typeof (TSource).Name, typeof (TDestination).Name), ex);
            }
        }
    }

    public interface ITypeConverter{
        object Convert(object source);
    }

    public class TypeConversionContainer<TSource, TDestination> : ITypeConverter{
        private readonly Func<TSource, TDestination> _conversion;

        public TypeConversionContainer(Func<TSource, TDestination> conversion){
            _conversion = conversion;
        }

        public object Convert(object source){
            try{
                return _conversion((TSource) source);
            }
            catch (Exception ex){
                throw new MapperException(string.Format("There was an error converting source {0} to target type", source.GetType().Name), ex);
            }
        }
    }

    public class MapperException : ApplicationException{
        public MapperException(string message) : base(message){}
        public MapperException(string message, Exception innerException) : base(message, innerException){}
    }

    internal class MapperConfigLoader{
        private readonly List<Mapper> _configuration = new List<Mapper>();

        private static IEnumerable<Type> GetMappers(){
            return
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from types in assembly.GetTypes()
                where typeof (Mapper).IsAssignableFrom(types) && !types.IsAbstract
                select types;
        }

        public void ActivateMappers(){
            var types = GetMappers().ToList();

            foreach (var type in types){
                try{
                    _configuration.Add((Mapper) Activator.CreateInstance(type));
                }
                catch (Exception ex){
                    throw new MapperException("There was an error creating mapper " + type.Name, ex);
                }
            }
        }
    }

    public abstract class Mapper{
        private readonly IMapperConfiguration _configuration;

        protected Mapper(){
            Map = new SetupMapping(_configuration);
            _configuration = ObjectMapper.CurrentConfiguration;
        }

        protected Mapper(IMapperConfiguration configuration){
            _configuration = configuration;
        }

        public SetupMapping Map { get; set; }

        protected void ConvertUsing<TFrom, TTo>(Func<TFrom, TTo> conversion){
            _configuration.AddConversion(conversion);
        }

        public class SetupMapping{
            private readonly IMapperConfiguration _configuration;

            public SetupMapping(IMapperConfiguration configuration){
                _configuration = configuration;
            }

            public SetupMap<TSource, TDestination> FromTo<TSource, TDestination>() where TSource : class
                where TDestination : class{
                return new MapTo<TSource>(_configuration).To<TDestination>();
            }

            public MapTo<TSource> From<TSource>() where TSource : class{
                return new MapTo<TSource>(_configuration);
            }

            public void WithConvention(Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention){
                _configuration.AddConvention(convention);
            }

            public class SetupConventionsOnManual<TSource, TDestination> where TSource : class
                where TDestination : class{
                private readonly ManualMap<TSource, TDestination> _map;

                public SetupConventionsOnManual(ManualMap<TSource, TDestination> map){
                    _map = map;
                }

                public void IgnoreConventions(){
                    _map.UseConventionMapping = false;
                }
            }

            public class MapTo<TSource> where TSource : class{
                internal readonly IMapperConfiguration Configuration;

                public MapTo(IMapperConfiguration configuration){
                    Configuration = configuration;
                }

                public SetupMap<TSource, TDestination> To<TDestination>() where TDestination : class{
                    var manualMap = new ManualMap<TSource, TDestination>(null, true, Configuration);
                    Configuration.AddMap(typeof (TSource), typeof (TDestination), manualMap);
                    return new SetupMap<TSource, TDestination>(manualMap, Configuration);
                }
            }

            public class SetupMap<TSource, TDestination> where TSource : class where TDestination : class{
                internal readonly IMapperConfiguration Configuration;
                private readonly ManualMap<TSource, TDestination> _map;

                public SetupMap(ManualMap<TSource, TDestination> map, IMapperConfiguration configuration){
                    _map = map;
                    Configuration = configuration;
                }

                public SetupMap<TSource, TDestination> IncludeFrom<T>(){
                    Debug.Assert(typeof (TSource).IsAssignableFrom(typeof (T)), "TSource must be assignable from T in order to share a property map");
                    Configuration.AddMap(typeof (T), typeof (TDestination), _map);
                    return this;
                }

                public SetupMap<TSource, TDestination> IncludeTo<T>(){
                    Debug.Assert(typeof (TDestination).IsAssignableFrom(typeof (T)), "TDestination must be assignable from T in order to share a property map");
                    Configuration.AddMap(typeof (T), typeof (TDestination), _map);
                    return this;
                }

                public SetupMap<TSource, TDestination> WithCustomConvention(
                    Func<PropertyInfo[], PropertyInfo[], IEnumerable<dynamic>> convention){
                    _map.Conventions.Add(convention);
                    return this;
                }

                public SetupMap<TSource, TDestination> WithCustomConversion<TFrom, TTo>(Func<TFrom, TTo> conversion){
                    _map.Conversions.Add(new KeyValuePair<Type, Type>(typeof (TFrom), typeof (TTo)), new TypeConversionContainer<TFrom, TTo>(conversion));
                    return this;
                }

                public SetupConventionsOnManual<TSource, TDestination> SetManually(Action<TSource, TDestination> map){
                    _map.ObjectMap = map;
                    return new SetupConventionsOnManual<TSource, TDestination>(_map);
                }

                public SetupMap<TSource, TDestination> CreateWith(Func<TSource, TDestination> activator){
                    _map.CustomActivator = activator;
                    return this;
                }

                public SetupMap<TSource, TDestination> Set(params Expression<Func<TDestination, object>>[] properties){
                    var ignoreList = typeof (TDestination).GetProperties().Select(x => x.Name).ToList();
                    var selectedProperties = properties.Select(GetPropertyNameFromLambda);
                    ignoreList.RemoveAll(selectedProperties.Contains);
                    _map.IgnoreProperties.AddRange(ignoreList);
                    return this;
                }

                public SetupMap<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] properties){
                    _map.IgnoreProperties.AddRange(properties.Select(GetPropertyNameFromLambda));
                    return this;
                }

                internal static string GetPropertyNameFromLambda(Expression<Func<TDestination, object>> expression){
                    var lambda = expression as LambdaExpression;
                    Debug.Assert(lambda != null, "Not a valid lambda epression");
                    MemberExpression memberExpression;

                    if (lambda.Body is UnaryExpression){
                        var unaryExpression = lambda.Body as UnaryExpression;
                        memberExpression = unaryExpression.Operand as MemberExpression;
                    }
                    else{
                        memberExpression = lambda.Body as MemberExpression;
                    }

                    Debug.Assert(memberExpression != null, "Please provide a lambda expression like 'x => x.PropertyName'");
                    var propertyInfo = (PropertyInfo) memberExpression.Member;

                    return propertyInfo.Name;
                }
            }
        }
    }

    internal class PropertyLookup {
        public IGetter Source { get; set; }
        public ISetter Destination { get; set; }
        public ITypeConverter Converter { get; set; }

        internal interface ISetter{ void Set(object item, object value); }
        internal interface IGetter{ object Get(object item); }

        internal class SetterInvoker<TObject, TProperty> : ISetter {
            private Action<TObject, TProperty> Setter { get; set; }

            public SetterInvoker(string propertyName){
                Setter = LambdaCompiler.CreateSetter<TObject, TProperty>(propertyName);
            }

            public void Set(object item, object value){
                Setter((TObject) item, (TProperty) value);
            }
        }

        internal class GetterInvoker<TObject, TProperty> : IGetter{
            private Func<TObject, TProperty> Getter { get; set; }

            public GetterInvoker(string propertyName){
                Getter = LambdaCompiler.CreateGetter<TObject, TProperty>(propertyName);
            }

            public object Get(object item){
                return Getter((TObject) item);
            }
        }
    }

    internal static class LambdaCompiler {
            public static Func<TObject, TProperty> CreateGetter<TObject, TProperty>(string propertyName) {
                var parameter = Expression.Parameter(typeof(TObject), "value");
                var property = Expression.Property(parameter, propertyName);
                return Expression.Lambda<Func<TObject, TProperty>>(property, parameter).Compile();
            }

            public static Action<TObject, TProperty> CreateSetter<TObject, TProperty>(string propertyName) {            
                var objectParameter = Expression.Parameter(typeof(TObject));
                var propertyParameter = Expression.Parameter(typeof(TProperty), propertyName);
                var getter = Expression.Property(objectParameter, propertyName);
                return Expression.Lambda<Action<TObject, TProperty>> ( Expression.Assign(getter, propertyParameter), objectParameter, propertyParameter).Compile();
            }

            internal static Func<T> CreateActivator<T>(){
                var constructor = typeof (T).GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
                if(constructor == null) throw new MapperException(string.Format("Cant create destination object {0}, no parameterless constructor available!", typeof(T).Name));
                return (Func<T>) Expression.Lambda(typeof(Func<T>), Expression.New(constructor)).Compile();
            }
        }
}