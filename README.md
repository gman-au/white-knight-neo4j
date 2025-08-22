# White Knight (Neo4j)
<p align="center">
<img style="border-radius:10px;" alt="white_knight" width="300" src="https://github.com/user-attachments/assets/1858bacb-ca86-4d79-8011-6fdf5ba80235" />
</p>

![NuGet Version](https://img.shields.io/nuget/v/White.Knight.Neo4j?label=White.Knight.Neo4j)

[![build-and-test](https://github.com/gman-au/white-knight-neo4j/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/gman-au/white-knight-neo4j/actions/workflows/build-and-test.yml)

## Summary
This repository is the [Neo4j](https://neo4j.com/) implementation of the [White Knight](https://github.com/gman-au/white-knight) repository abstraction library.

## Installation / usage
### Set up repositories
* Define `IRepository` implementations by deriving from the `Neo4JKeylessRepositoryBase<T>` base class, for convenience, using the `IsNeo4JRepository` attribute for automatated dependency injection:
```csharp
    [IsNeo4JRepository]
    public class CustomerRepository(Neo4JRepositoryFeatures<Customer> repositoryFeatures)
        : Neo4JKeylessRepositoryBase<Customer>(repositoryFeatures)
    {
        public override Expression<Func<Customer, object>> DefaultOrderBy()
        {
            return o => o.CustomerId;
        }
    }
```

### Add dependency injection
* With the repository implementations marked with the attribute as above, add the following to your DI code:
```csharp

    private static readonly Assembly RepositoryAssembly = Assembly.GetAssembly(typeof(CustomerRepository));

    public static IServiceCollection AddMyNeo4jRepositories(
        this IServiceCollection services, 
        IConfigurationRoot configuration
    )
    {
        ServiceCollection
            .AddRepositoryFeatures<Neo4JRepositoryConfigurationOptions>(Configuration)
            .AddNeo4JRepositories(Configuration)
            .AddAttributedNeo4JRepositories(RepositoryAssembly)
            .AddNeo4JRepositoryFeatures(Configuration);

        return services;
    }

```

## Caveats
### New objects must have unique IDs (if required) set _before insertion_
* The Neo4j driver will simply take domain class objects and map them to a property dictionary before inserting them as a Neo4j entity; thus, it will not respect component model attributes like `[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, which EF, for example, would use to retrieve / generate an ID during record insertion.

  * As such, where a new, unique ID is to be set for a new record, it must be determined **prior** to insertion i.e.:  
  `Customer.CustomerId = Guid.NewGuid();`

> [!TIP]
> It is worth bearing in mind that Neo4j does not require unique IDs for a given entity to be inserted, as it maintains its own unique entity IDs in the background.

### POCO collections must be initialised
* For domain library POCOs, virtual collections (used in Entity Framework, for example, to bind child relationships, for example) _need to be initialised_ as part of their class code i.e.
```csharp
    public class Customer {

        ...

        public virtual IList<Orders> Orders { get; set; } = new List<Order>();

    }
```
Consider the following query code:
```csharp
var results =
    await
        repository
            .QueryAsync
            (
                new CustomerSpecByCustomerNumber(200)
                    .ToQueryCommand()
                    .WithRelationshipStrategy(
                        new RelationshipNavigation<Customer, Order>(
                            "CREATED_ORDER",
                            (c, o) => c.Orders.Add(o)))
            );
```
This needs to be done where a **relationship navigation strategy** will be used, as the `Setter` requires the target object to be initialised prior (otherwise `c.Orders` will throw a `NullReferenceException`).
