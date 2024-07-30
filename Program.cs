using HoneyRaesAPI.Models;
using System.Text.Json.Serialization;

List<Customer> customers = new List<Customer>
{
    new Customer
    {
        Id = 1,
        Name = "Alice Smith",
        Address = "123 Main St"
    },

    new Customer
    {
        Id = 2,
        Name = "Bob Jones",
        Address = "456 Elm St"
    },

    new Customer
    {
        Id = 3,
        Name = "Charlie Johnson",
        Address = "789 Oak St"
    }
};

List<Employee> employees = new List<Employee>
{
    new Employee
    {
        Id = 1,
        Name = "Eve Adams",
        Specialty = "Plumbing"
    },

    new Employee
    {
        Id = 2,
        Name = "Frank Brown",
        Specialty = "Electrical"
    }
};

List<ServiceTicket> serviceTickets = new List<ServiceTicket>
{
    new ServiceTicket
    {
        Id = 1,
        CustomerId = 1,
        EmployeeId = 1,
        Description = "Fix leaky faucet",
        Emergency = true,
        DateCompleted = new DateTime(2024, 6, 1)
    },

    new ServiceTicket
    {
        Id = 2,
        CustomerId = 2,
        EmployeeId = 1,
        Description = "Repair broken light switch",
        Emergency = false,
        DateCompleted = new DateTime(2024, 6, 27)
    },

    new ServiceTicket
    {
        Id = 3,
        CustomerId = 3,
        Description = "Install new dishwasher",
        Emergency = true
    },

    new ServiceTicket
    {
        Id = 4,
        CustomerId = 1,
        EmployeeId = 2,
        Description = "Replace electrical outlet",
        Emergency = false,
        DateCompleted = new DateTime(2024, 6, 15)
    },

    new ServiceTicket
    {
        Id = 5,
        CustomerId = 2,
        Description = "Fix garage door opener",
        Emergency = false
    }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Fix for cycle error
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoints
app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    serviceTicket.Customer = customers.FirstOrDefault(c => c.Id == serviceTicket.CustomerId);
    return Results.Ok(serviceTicket);
});

app.MapPost("/servicetickets", (ServiceTicket serviceTicket) =>
{
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/servicetickets/{id}", (int id) =>
{
    serviceTickets.RemoveAll(st => st.Id == id);
});

app.MapPut("/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    if (id != serviceTicket.Id)
    {
        return Results.BadRequest();
    }
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapPost("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    ticketToComplete.DateCompleted = DateTime.Today;
});

app.MapGet("/servicetickets/emergency/incomplete", () =>
{
    return serviceTickets.Where(ticket => ticket.Emergency && ticket.DateCompleted == null);
});

app.MapGet("/servicetickets/unassigned", () =>
{
    return serviceTickets.Where(ticket => ticket.EmployeeId == null);
});

app.MapGet("/servicetickets/complete", () =>
{
    var completedTickets = serviceTickets
        .Where(t => t.DateCompleted.HasValue)
        .OrderBy(t => t.DateCompleted.Value)
        .ToList();

    return completedTickets;
});

app.MapGet("/servicetickets/priority", () =>
{
    var incompleteTickets = serviceTickets
        .Where(t => t.DateCompleted == null)
        .OrderByDescending(t => t.Emergency)
        .ThenBy(t => t.EmployeeId == null)
        .ToList();

    return incompleteTickets;
});

app.MapGet("/employees", () =>
{
    return employees;
});

app.MapGet("/employees/{id}", (int id) =>
{
    Employee employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee == null)
    {
        return Results.NotFound();
    }
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    return Results.Ok(employee);
});

app.MapGet("/employees/available", () =>
{
    var assignedEmployeeIds = serviceTickets
        .Where(ticket => ticket.DateCompleted == null && ticket.EmployeeId.HasValue)
        .Select(ticket => ticket.EmployeeId.Value)
        .Distinct();

    var unassignedEmployees = employees
        .Where(employee => !assignedEmployeeIds.Contains(employee.Id));

    return unassignedEmployees;
});

app.MapGet("/employees/{id}/customers", (int id) =>
{
    var customerIds = serviceTickets
        .Where(ticket => ticket.EmployeeId == id)
        .Select(ticket => ticket.CustomerId)
        .Distinct();

    var employeeCustomers = customers
        .Where(customer => customerIds.Contains(customer.Id));

    return employeeCustomers;
});

app.MapGet("/employee-of-the-month", () =>
{
    DateTime firstDayOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    DateTime firstDayOfLastMonth = firstDayOfCurrentMonth.AddMonths(-1);
    DateTime lastDayOfLastMonth = firstDayOfCurrentMonth.AddDays(-1);

    var ticketsLastMonth = serviceTickets
        .Where(t => t.DateCompleted.HasValue &&
                    t.DateCompleted.Value >= firstDayOfLastMonth &&
                    t.DateCompleted.Value <= lastDayOfLastMonth)
        .ToList();

    var employeeTicketCounts = ticketsLastMonth
        .GroupBy(t => t.EmployeeId)
        .Select(t => new { EmployeeId = t.Key, TicketCount = t.Count() })
        .ToList();

    var topEmployeeId = employeeTicketCounts
        .OrderByDescending(e => e.TicketCount)
        .FirstOrDefault()?.EmployeeId;

    var topEmployee = employees.FirstOrDefault(e => e.Id == topEmployeeId);
    return topEmployee;
});

app.MapGet("/customers", () =>
{
    return customers;
});

app.MapGet("/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId == id).ToList();
    return Results.Ok(customer);
});

app.MapGet("/customers/inactive", () =>
{
    var oneYearAgo = DateTime.Now.AddYears(-1);
    var inactiveCustomers = customers.Where(customer =>
        !serviceTickets.Any(ticket =>
            ticket.CustomerId == customer.Id &&
            ticket.DateCompleted.HasValue &&
            ticket.DateCompleted.Value > oneYearAgo
        )
    );
    return inactiveCustomers;
});

app.Run();
