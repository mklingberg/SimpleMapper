<img align="center" src="https://cloud.githubusercontent.com/assets/10036003/5346059/e56140a6-7f1c-11e4-88c9-59d570fa2ca7.png">

SimpleMapper is a lightweight, highly optimized library for mapping between entities such as domain and view models or domain and service contracts.

No one likes writing mapping code, its boring to write and takes time and resources to maintain. Mapping with SimpleMapper is a breeze. This example demostrates the simple .MapTo<> syntax.

```csharp

var model = Session.Get<Customer>(id).MapTo<CustomerModel>();

```

## Installation

Simply download SimpleMapper.cs and add it to your project. Available on NuGet soon...

## Mapping

SimpleMapper supports convention based mapping, and aslong as you stick to a convention, you can map it automatically with SimpleMapper. When no convention applies its easy to create a map manually using the fluent API and the Mapper base class.

```csharp

public class CustomerMapper : Mapper
{
	public CustomerMapper()
	{
		Map.From<Customer>().To<CustomerModel>().SetManually((customer, model) => 
		{
			model.Orders = string.Join(",", customer.Orders.Select(x => x.Name));
		});
	}
}

```

## Convertions

SimpleMapper uses convertions to handle mapping between entities with same name but different type. It has built in support for convertions between strings, dates, numbers, booleans and enums.
You can provide custom convertions for complex types and interfaces used by your models. The example below adds a global convertion between an INamedEntity and a string.

```csharp

public class NamedEntityToStringMapper : Mapper
{
	public static readonly Func<INamedEntity, string> InterfaceToStringConversion = x => x.Name;

	public InterfaceToStringMapper()
	{
		Configure.WithConversion(InterfaceToStringConversion);
	}
}

```

## Conventions

Custom conventions can easily be defined with LINQ.

```csharp

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

```

## Initialization

SimpleMapper is initialized automatically if you just start mapping. If you want to customize the behavior or create entity maps on application startup you can do so with ObjectMapper.Initialize.

```csharp

ObjectMapper.Initialize(x =>
{
	x.DefaultActivator = ObjectFactory.GetInstance;
	x.CreateMissingMapsAutomatically = true;
	x.Scanner.ScanAssembliesContainingType<Customer>();
	x.Scanner.ScanAllAssembliesInBaseFolder = false;
});

```

By default SimpleMapper uses Activator.CreateInstance to create maps/classes. You can configure any IOContainer for dependency injection into your entity mappers using the DefaultActivator delegate shown above.

## Optimization

SimpleMapper is optimized to perform your mapping super fast. As many other mapping frameworks rely on reflection to do the heavy lifting, SimpleMapper uses compiled lambdas for the actual mapping and by doing this achievs a speedup by 5-10 times.

## Copyright

Copyright © 2014 Marcus Klingberg and contributors

## License

SimpleMapper is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to license.txt for more information.

