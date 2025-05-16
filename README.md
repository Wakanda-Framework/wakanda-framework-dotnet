# Wakanda Framework Consumer Guide

## Overview
This guide shows how to **install**, **configure**, and **consume** Wakanda Framework in your own microservice.

### Installation
To install the framework:

```bash
dotnet add package org.wakanda.framework
```

### Configuration

1. In your `Program.cs` or where you setup services, add:

```csharp
using org.wakanda.framework.Extensions;

// Register database context first
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Core services (repos & services)
builder.Services.AddWakandaFrameworkCore<MasterDbContext>();

// Cache (optional)
builder.Services.AddWakandaFrameworkCaching(builder.Configuration);

// Message broker (optional)
builder.Services
    .AddWakandaFrameworkMessageBroker(builder.Configuration)
    .AddWakandaFrameworkKafkaBroker();
```

2. Add the following sections to your `appsettings.json`:

```json
{
  "CacheSettings": {
    "Enabled": true,
    "Provider": "Redis",
    "DefaultExpiryMinutes": 30
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "MessageBrokerSettings": {
    "Enabled": true,
    "BootstrapServers": "localhost:9092",
    "SaslUsername": "",
    "SaslPassword": "",
    "SecurityProtocol": "Plaintext"
  }
}
```

## Usage Examples

### Creating Entities

```csharp
using org.wakanda.framework.Attributes;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Enums;

// Make an entity cacheable 
[Cacheable]
// Configure which events are published to message broker
[Publish(PublishOn = Action.Create | Action.Update)]
public class Customer : BaseEntity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    
    // Properties marked with [Searchable] will be included in search operations
    [Searchable]
    public string PhoneNumber { get; set; }
}
```

### Creating a Repository

```csharp
using org.wakanda.framework.Repositories.Interface;

// Create interface
public interface ICustomerRepository : IBaseRepository<Customer, CustomerFilter, MasterDbContext, SlaveDbContext>
{
    Task<Customer> GetByEmailAsync(string email);
}

// Implement repository
public class CustomerRepository : BaseRepository<Customer, CustomerFilter, MasterDbContext, SlaveDbContext>, ICustomerRepository
{
    public CustomerRepository(MasterDbContext masterContext, SlaveDbContext slaveContext) 
        : base(masterContext, slaveContext)
    {
    }
    
    public async Task<Customer> GetByEmailAsync(string email)
    {
        return await _masterDbContext.Customers
            .FirstOrDefaultAsync(c => c.Email == email && !c.IsDeleted);
    }
}
```

### Using Cache and Message Broker

```csharp
using org.wakanda.framework.Cache;
using org.wakanda.framework.Cache.Interface;

// Inject these services into your own services
public class CustomerService
{
    private readonly ICustomerRepository _customerRepo;
    private readonly ICacheService _cacheService;
    private readonly IBaseEntityCacheService<Customer> _customerCache;
    
    public CustomerService(
        ICustomerRepository customerRepo,
        ICacheService cacheService, 
        IBaseEntityCacheService<Customer> customerCache)
    {
        _customerRepo = customerRepo;
        _cacheService = cacheService;
        _customerCache = customerCache;
    }
    
    // Method implementation...
}
```

### Working with Configurations

```csharp
using org.wakanda.framework.Cache.Interface;
using org.wakanda.framework.MessageBroker.Interface;
using org.wakanda.framework.Services;
using Microsoft.Extensions.Options;
using org.wakanda.framework.Settings;
using org.wakanda.framework.Utilities;

public class ConfigExample
{
    private readonly CacheSettings _cacheSettings;
    private readonly MessageBrokerSettings _brokerSettings;
    
    public ConfigExample(
        IOptions<CacheSettings> cacheOptions,
        IOptions<MessageBrokerSettings> brokerOptions)
    {
        _cacheSettings = cacheOptions.Value;
        _brokerSettings = brokerOptions.Value;
    }
}
```