#Introducing Mau
Mau simplifies mapping objects to database queries.
##Features
* No database-specific implementation details. Should work with any ADO.NET provider (tested with SQL Server and MySQL).
* No configuration.
* Uses regular SQL - still the best domain specific language for relational data.
* Supports parameterized text queries or stored procedures.
* Automatically maps query results to Enumerable of:
  * Strongly-typed objects, or
  * Dynamic objects.
* Transaction support through Context and Unit of Work objects.
* IoC Friendly

##Code Samples
####Get an Context object:
    var factory = new AppConfigConnectionFactory("connection-string-name");
    var context = new AdoNetContext(factory);
####Execute a Query and Map the Result to a Strongly Typed Enumerable
    var products = context.Query<Cat>(
        "SELECT * FROM Cats WHERE BreedId == @BreedId",
        new { BreedId = 1 });
####Execute a Query and Map the Result to an Enumerable of Dynamic Objects
    var products = context.Query(
        "SELECT * FROM Cats WHERE BreedId == @BreedId",
        new { BreedId = 2 });
####Execute a Command That Returns a Scalar Value
    var breedId = context.Scalar<int>(
        "INSERT INTO Breed(Name) VALUES(@Name); SELECT SCOPE_IDENTITY();",
        new { Name = "Egyptian Mau" });
####Execute a Command That Doesn't Return Anything
    context.Execute(
        "DELETE FROM Cat WHERE BreedId < @MinBreedId,
        new { MinBreedId = 2 });
####Execute Multiple Commands in a Transaction
    var breed1 = new Breed { Name = "Egyptian Mau" };
    var breed2 = new Breed { Name = "Arabian Mau" };
    
    var cat1 = new Cat
    {
        Name = "Pharoh",
        Age = 4
    };
    
    var cat2 = new Cat
    {
        Name = "Tut",
        Age = 2
    };
    
    var cat3 = new Cat
    {
        Name = "Anas",
        Age = 8
    };
    
    using(var uow = context.CreateUnitOfWork())
    {
        var sql = "INSERT INTO Breed(Name) VALUES(@Name); SELECT @@SCOPE_IDENTITY()";
        
        breed1.BreedId = context.Scalar<int>(sql, new { Name = breed1.Name })
        breed2.BreedId = context.Scalar<int>(sql, new { Name = breed2.Name })
        
        cat1.BreedId = breed1.BreedId;
        cat2.BreedId = breed1.BreedId;
        cat3.BreedId = breed2.BreedId;
        
        sql = @"INSERT INTO Cat(Name, Age, BreedId) VALUES(@Name, @Age, @BreedId)";
        
        context.Execute(sql, new
        {
            Name = cat1.Name,
            Age = cat1.Age,
            BreedId = cat1.BreedId
        });
        
        context.Execute(sql, new
        {
            Name = cat2.Name,
            Age = cat2.Age,
            BreedId = cat2.BreedId
        });
        
        context.Execute(sql, new
        {
            Name = cat3.Name,
            Age = cat3.Age,
            BreedId = cat3.BreedId
        });
        
        uow.SaveChanges();
    }
#### IoC Friendliness
    var container = new UnityContainer();
    container.RegisterType<IConnectionFactory, AppConfigConnectionFactory>(new ContainerControlledLifetimeManager(), new InjectionConstructor("ConnectionStringName"));
    container.RegisterType<IAdoNetContext, AdoNetContext>(new HierarchicalLifetimeManager());
