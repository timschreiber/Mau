#Mau Simplifies Mapping Objects to Database Queries
##Features
* No database-specific implementation details. Should work with any ADO.NET provider (tested with SQL Server and MySQL).
* No configuration.
* Uses regular SQL - still the best domain specific language for relational data.
* Supports parameterized text queries or stored procedures.
* Automatically maps query results to Enumerable of:
  * Strongly-typed objects, or
  * Dynamic objects.
* Transaction support through Context and Unit of Work objects.

##Code Samples
#### Get an AdoNetContext object:
    var factory = new AppConfigConnectionFactory("connection-string-name");
    var context = new AdoNetContext(factory);
#### Execute a query and map the results to an a strongly-typed Enumerable:
    public class Cat
    {
        public Guid Id { get; set; }
        public int? Age { get; set; }
        public string Name { get; set; }
        public float? Weight { get; set; }
    }
    
    var cats = context.Query<Cat>("SELECT Age, Id, Name, Weight FROM Cat WHERE Id = @Id", new { Id = guid });
    
    cats.Count()
        .IsEqualTo(1);
    
    cats.First().Age
        .IsNull();
    
    cats.First().Id
        .IsEqualTo(guid);
 
