using HoneyRaesAPI.Models;

List<Customer> customers = new List<Customer>
{
    new Customer { Id = 1, Name = "Alice Smith", Address = "123 Main St" },
    new Customer { Id = 2, Name = "Bob Jones", Address = "456 Elm St" },
    new Customer { Id = 3, Name = "Charlie Johnson", Address = "789 Oak St" }
};

List<Employee> employees = new List<Employee>
{
    new Employee { Id = 1, Name = "Eve Adams", Specialty = "Plumbing" },
    new Employee { Id = 2, Name = "Frank Brown", Specialty = "Electrical" }
};

List<ServiceTicket> serviceTickets = new List<ServiceTicket>
{
    new ServiceTicket { Id = 1, CustomerId = 1, EmployeeId = 1, Description = "Fix leaky faucet", Emergency = false, DateCompleted = new DateTime(2024, 7, 1) },
    new ServiceTicket { Id = 2, CustomerId = 2, EmployeeId = 2, Description = "Repair broken light switch", Emergency = true },
    new ServiceTicket { Id = 3, CustomerId = 3, Description = "Install new dishwasher", Emergency = false },
    new ServiceTicket { Id = 4, CustomerId = 1, EmployeeId = 2, Description = "Replace electrical outlet", Emergency = true, DateCompleted = new DateTime(2024, 7, 15) },
    new ServiceTicket { Id = 5, CustomerId = 2, Description = "Fix garage door opener", Emergency = false }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    return serviceTickets.FirstOrDefault(st => st.Id == id);
});

app.Run();
