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
####Get an AdoNetContext object:
    var factory = new AppConfigConnectionFactory("connection-string-name");
    var context = new AdoNetContext(factory);
####Insert Records Using a Transaction
    Category category1 = new Category { Name = "Category 1" };
    Category category2 = new Category { Name = "Category 2" };
    Product product1 = new Product
    {
        ProductId = "PROD123",
        Name = "Product 123",
        Description = "The first Product",
        Price = 19.99M
    };
    Product product2 = new Product
    {
        ProductId = "PROD234",
        Name = "Product 234",
        Description = "The second Product",
        Price = 24.99M
    };
    Product product3 = new Product
    {
        ProductId = "PROD345",
        Name = "Product 345",
        Description = "The third Product",
        Price = 29.99M
    };
    
    using(var uow = context.CreateUnitOfWork())
    {
        var sql = "INSERT INTO Category(Name) VALUES(@Name); SELECT @@SCOPE_IDENTITY()";
        
        category1.CategoryId = context.Scalar<int>(sql, new { Name = category1.Name })
        category2.CategoryId = context.Scalar<int>(sql, new { Name = category2.Name })
        
        product1.CategoryId = category1.CategoryId;
        product2.CategoryId = category2.CategoryId;
        product3.CategoryId = category2.CategoryId;
        
        sql = @"
            INSERT INTO Product(ProductId, CategoryId, Name, Description, Price)
            VALUES(@ProductId, @CategoryId, @Name, @Description, @Price)";
        
        context.Execute(sql, new
        {
            ProductId = product1.ProductId,
            CategoryId = product1.CategoryId,
            Name = product1.Name,
            Description = product1.Description,
            Price = product1.Price
        });
        
        context.Execute(sql, new
        {
            ProductId = product2.ProductId,
            CategoryId = product2.CategoryId,
            Name = product2.Name,
            Description = product2.Description,
            Price = product2.Price
        });
        
        context.Execute(sql, new
        {
            ProductId = product2.ProductId,
            CategoryId = product2.CategoryId,
            Name = product2.Name,
            Description = product2.Description,
            Price = product2.Price
        });
        
        uow.SaveChanges();
    }
