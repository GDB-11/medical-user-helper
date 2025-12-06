# Medical User Helper

> **A showcase of Railway-Oriented Programming in C# using [BindSharp](https://github.com/BindSharp/BindSharp)**
>
> Demonstrates clean, type-safe error handling across every layer of a real cross-platform desktop application.

[![Built with BindSharp](https://img.shields.io/badge/Built%20with-BindSharp-blue?style=for-the-badge)](https://github.com/BindSharp/BindSharp)
[![Photino](https://img.shields.io/badge/Photino-Cross--Platform-green?style=for-the-badge)](https://www.tryphotino.io/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=for-the-badge)](https://dotnet.microsoft.com/)

---

## 🚂 Why This Project Exists

This is a **production example** of Railway-Oriented Programming (ROP) with [BindSharp](https://github.com/BindSharp/BindSharp) throughout an entire application stack:

- ✅ **UI Layer** - Message routing with `Tap`, `TapError`, and `BindIf`
- ✅ **Application Layer** - Business logic with `Ensure` chains and `Bind` composition  
- ✅ **Infrastructure Layer** - Database operations with `TryAsync` and error mapping
- ✅ **Domain Layer** - Validation pipelines and type-safe errors

**The domain?** A developer tool that generates algorithmically valid medical credential numbers (NPI, DEA, License Numbers) for testing purposes.

**The real value?** Seeing BindSharp patterns in action across a complete, cross-platform desktop application.

---

## 🎯 BindSharp Patterns Demonstrated

### 1. **Validation Pipelines with `Ensure`**

Traditional validation is messy:
```csharp
// ❌ The old way - nested ifs, unclear flow
public bool ValidateNpi(string npi, out string error)
{
    if (string.IsNullOrEmpty(npi))
    {
        error = "NPI cannot be null or empty";
        return false;
    }
    
    if (npi.Length != 10)
    {
        error = "NPI must be exactly 10 digits";
        return false;
    }
    
    if (!long.TryParse(npi, out _))
    {
        error = "NPI must contain only numeric digits";
        return false;
    }
    
    if (!ValidateLuhnCheckDigit(npi))
    {
        error = "NPI failed Luhn check digit validation";
        return false;
    }
    
    error = null;
    return true;
}
```

With BindSharp's ROP approach:
```csharp
// ✅ The BindSharp way - clear, linear, composable
public Result<ValidateNationalProviderIdentifierResponse, NationalProviderIdentifierError> 
    ValidateNpi(string npi) =>
    ValidateNpiString(npi)
        .Ensure(
            n => n.Length == 10,
            new NationalProviderIdentifierValidationError("NPI must be exactly 10 digits"))
        .Ensure(
            n => long.TryParse(n, out _),
            new NationalProviderIdentifierValidationError("NPI must contain only numeric digits"))
        .Ensure(
            n => ValidateLuhnCheckDigit(n),
            new NationalProviderIdentifierValidationError("NPI failed Luhn check digit validation"))
        .Map(_ => new ValidateNationalProviderIdentifierResponse(true));
```

**Why this is better:**
- 🎯 Each validation step is explicit and independent
- 🔍 Easy to see what's being validated
- 🛑 Stops at the first failure automatically
- ✅ Type-safe errors with specific messages
- 🧪 Each step is independently testable

---

### 2. **Exception Handling with `TryAsync`**

Traditional database error handling:
```csharp
// ❌ The old way - try-catch hell
public async Task<bool> AddAsync(NationalProviderIdentifierNumber npiNumber)
{
    try
    {
        var affectedRows = await ExecuteNonQueryAsync(_connection, sql, npiNumber);
        
        if (affectedRows == 0)
        {
            _logger.LogError("No rows affected when inserting NPI");
            return false;
        }
        
        return true;
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error inserting NPI");
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error inserting NPI");
        return false;
    }
}
```

With BindSharp's `TryAsync`:
```csharp
// ✅ The BindSharp way - exceptions become typed errors
public async Task<Result<Unit, NationalProviderIdentifierError>> 
    AddAsync(NationalProviderIdentifierNumber npiNumber) =>
    await ResultExtensions.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection, NationalProviderIdentifierSql.Insert, npiNumber),
            errorFactory: ex => new NationalProviderIdentifierInsertError(ex.Message, ex)
        )
        .BindAsync(affectedRows => ValidateAffectedRows(
            affectedRows, 
            msg => new NationalProviderIdentifierInsertError(msg),
            "Error inserting the NPI number."
        ));
```

**Why this is better:**
- 🔒 Exceptions converted to **typed domain errors**
- 📊 Validation continues **in the same pipeline**
- 🧹 No nested try-catch blocks
- 🎯 Clear error types (`NationalProviderIdentifierInsertError`)
- 🔄 Composable with other Result operations

---

### 3. **Error Transformation with `MapError`**

Chain operations across error type boundaries:
```csharp
public async Task<Result<CreateLicenseNumberResponse, LicenseNumberError>> 
    CreateLicenseNumber(string stateCode, string lastName, LicenseNumberType licenseNumberType)
{
    var licenseNumber = new LicenseNumber
    {
        LicenseNumberId = Guid.CreateVersion7(),
        LicenseNumberValue = GenerateLicenseNumber(stateCode, lastName, licenseNumberType),
        CreatedAt = DateTime.UtcNow
    };
    
    return await IsStringEmpty<LicenseNumberNoStateCodeError>(stateCode)
        .MapError(LicenseNumberError (error) => error)  // 🔄 Convert to domain error
        .Bind(_ => IsStringEmpty<LicenseNumberNoLastNameError>(lastName)
            .MapError(LicenseNumberError (error) => error))  // 🔄 Convert again
        .BindAsync(async _ => await _licenseNumberRepository.AddAsync(licenseNumber))
        .MapAsync(_ => new CreateLicenseNumberResponse
        {
            LicenseNumber = licenseNumber.LicenseNumberValue
        });
}
```

**Why this matters:**
- 🎭 Different layers have different error types
- 🔄 `MapError` seamlessly converts between them
- 🏗️ Maintains clean architecture boundaries
- ✅ Type-safe error propagation

---

### 4. **Observability with `Tap` and `TapError`**

Track what's happening without breaking the pipeline:
```csharp
public void RouteMessage(PhotinoWindow window, string message)
{
    ValidateMessage(message)
        .Tap(msg => _logger.LogDebug("Message validated: {0}", msg))  // ✅ Success logging
        .Bind(ParseMessage)
        .Tap(parsed => _logger.LogDebug("Parsed command: {0}", parsed.Command))  // ✅ Success logging
        .TapError(error => _logger.LogWarning("Invalid format: {0}", error))  // ⚠️ Error logging
        .Bind(parsed => GetHandler(parsed.Command)
            .Map(handler => (handler, parsed.Payload)))
        .Tap(tuple => _logger.LogDebug("Routing to {0}", tuple.handler.GetType().Name))  // ✅ Success logging
        .TapError(error => _logger.LogWarning("Error handling message: {0}", error))  // ⚠️ Error logging
        .Match(
            result => ExecuteHandler(window, result),
            error => HandleError(window, error)
        );
}
```

**Why this is powerful:**
- 📊 Full observability throughout the pipeline
- ✅ `Tap` executes only on success
- ⚠️ `TapError` executes only on failure
- 🔄 Result flows through unchanged
- 🎯 Clean separation: logging vs business logic

---

### 5. **Conditional Processing with `BindIf`**

Execute operations only when needed:
```csharp
/// <summary>
/// Extract JSON from payload that may be in "request:id:json" format
/// </summary>
protected static Result<string, MessageHandlerError> ExtractJsonFromPayload(string payload) =>
    ValidateIfPayloadIsEmpty(payload)
        .Map(trimmedPayload => trimmedPayload.TrimStart())
        .BindIf(
            // If payload doesn't start with '{' or '[', then extract
            trimmed => !(trimmed.StartsWith('{') || trimmed.StartsWith('[')),
            trimmed => ExtractJsonAfterPrefix(trimmed)  // Only runs if predicate is true
        );
```

**Why this is elegant:**
- ✅ Conditional logic stays **in the pipeline**
- 🔀 No breaking the flow with if statements
- 🎯 Operation only runs when needed
- 📖 Self-documenting intent

---

### 6. **Reusable Patterns with Generic Error Types**

Build composable validation utilities:
```csharp
protected static Result<Unit, TError> ValidateAffectedRows<TError>(
    int affectedRows,
    Func<string, TError> errorFactory,
    string errorMessage) =>
    affectedRows > 0
        ? Result<Unit, TError>.Success(Unit.Value)
        : Result<Unit, TError>.Failure(errorFactory(errorMessage));
```

**Usage across repositories:**
```csharp
// NPI Repository
.BindAsync(affectedRows => ValidateAffectedRows(
    affectedRows,
    msg => new NationalProviderIdentifierInsertError(msg),
    "Error inserting NPI number"
));

// DEA Repository  
.BindAsync(affectedRows => ValidateAffectedRows(
    affectedRows,
    msg => new DeaInsertError(msg),
    "Error inserting DEA number"
));
```

**Why this rocks:**
- ♻️ Write validation logic **once**
- 🎯 Reuse across **different error types**
- 🔒 Type-safe with generics
- 🧹 DRY principle in action

---

### 7. **Multi-Step Workflows**

Complex operations remain readable:
```csharp
public async Task<Result<CreateDrugEnforcementAdministrationNumberResponse, DeaRegistrationNumberError>>
    CreateDrugEnforcementAdministrationNumber(string lastName)
{
    var deaNumber = new DeaRegistrationNumber
    {
        DeaRegistrationNumberId = Guid.CreateVersion7(),
        DeaRegistrationNumberValue = GenerateDeaNumber(lastName),
        CreatedAt = DateTime.UtcNow
    };
    
    return await IsStringEmpty<DeaRegistrationNumberNoLastNameError>(lastName)
        .MapError(DeaRegistrationNumberError (error) => error)  // Step 1: Validate input
        .BindAsync(async _ => 
            await _deaRegistrationNumberRepository.AddAsync(deaNumber))  // Step 2: Save to DB
        .MapAsync(_ => new CreateDrugEnforcementAdministrationNumberResponse
        {
            DrugEnforcementAdministrationNumber = deaNumber.DeaRegistrationNumberValue
        });  // Step 3: Map to response
}
```

**Benefits:**
- 📖 Read top-to-bottom like a recipe
- 🔄 Each step transforms the result
- 🛑 Stops at first failure
- ✅ Type-safe all the way through

---

## 🏗️ Application Architecture

BindSharp flows through every layer:
```
┌─────────────────────────────────────────────────────────┐
│                   Photino Window                        │
│              (Cross-Platform Desktop)                   │
│         Windows | macOS | Linux                         │
└────────────────────┬────────────────────────────────────┘
                     │ JavaScript Messages
                     ▼
┌─────────────────────────────────────────────────────────┐
│                  MessageRouter                          │
│   ValidateMessage → ParseMessage → GetHandler           │
│        .Tap(log) → .TapError(log) → .Match()            │
│                                                          │
│   Pattern: Message routing with full observability      │
└────────────────────┬────────────────────────────────────┘
                     │ Result<Command, MessageError>
                     ▼
┌─────────────────────────────────────────────────────────┐
│              Application Services                       │
│   - NationalProviderIdentifierService                   │
│   - DrugEnforcementAdministrationService                │
│   - LicenseNumberService                                │
│                                                          │
│   Pattern: Validation chains with .Ensure()             │
│   Pattern: Multi-step workflows with .Bind/.BindAsync   │
└────────────────────┬────────────────────────────────────┘
                     │ Result<Entity, DomainError>
                     ▼
┌─────────────────────────────────────────────────────────┐
│             Infrastructure Layer                        │
│   - Repositories (Dapper + SQLite)                      │
│   - BaseDatabaseService                                 │
│                                                          │
│   Pattern: Exception handling with .TryAsync()          │
│   Pattern: Generic validation with ValidateAffectedRows │
└─────────────────────────────────────────────────────────┘
```

**Every layer returns `Result<T, TError>`** - errors flow naturally up the stack.

---

## 📚 Complete BindSharp Patterns Reference

| Pattern | When to Use | Example Location |
|---------|-------------|------------------|
| **`Ensure`** | Validation chains | `NationalProviderIdentifierService.ValidateNpi` |
| **`TryAsync`** | Exception handling | `NationalProviderIdentifierRepository.AddAsync` |
| **`Bind` / `BindAsync`** | Chain operations that can fail | `DrugEnforcementAdministrationService` |
| **`Map` / `MapAsync`** | Transform success values | Throughout service layer |
| **`MapError`** | Convert error types | `LicenseNumberService.CreateLicenseNumber` |
| **`Tap` / `TapAsync`** | Success side effects (logging) | `MessageRouter.RouteMessage` |
| **`TapError` / `TapErrorAsync`** | Error side effects (logging) | `MessageRouter.RouteMessage` |
| **`BindIf`** | Conditional processing | `MessageHandler.ExtractJsonFromPayload` |
| **`Match`** | Handle both tracks | UI boundary, message routing |
| **`Unit`** | Operations with no return value | Database operations |

---

## 🎓 What You'll Learn

### From This Codebase

1. **How to structure ROP across layers**
   - UI message routing
   - Application service composition
   - Infrastructure error handling

2. **Real-world BindSharp patterns**
   - Not toy examples - actual production code
   - Complex workflows simplified
   - Type-safe error handling throughout

3. **Clean architecture with Results**
   - Domain errors stay in domain
   - Infrastructure errors stay in infrastructure
   - `MapError` bridges the layers

4. **Healthcare-specific validation**
   - Luhn algorithm for NPI numbers
   - DEA checksum validation
   - State-specific license formats

### Why Healthcare Domain?

Healthcare demands **robust error handling**:
- ❌ Silent failures are unacceptable
- ✅ All errors must be logged and handled
- 📊 Audit trails are critical
- 🔒 Validation must be explicit

This makes it perfect for demonstrating Railway-Oriented Programming.

---

## 🔧 Technologies

- **[BindSharp](https://github.com/BindSharp/BindSharp)** - Railway-oriented programming
- **[Photino](https://www.tryphotino.io/)** - Cross-platform desktop (Chromium + .NET)
- **[SQLite](https://www.sqlite.org/)** - Embedded database
- **[Dapper](https://github.com/DapperLib/Dapper)** - Micro-ORM
- **.NET 8** - Modern C# features

### Cross-Platform Desktop with Photino

This app runs natively on:
- ✅ Windows
- ✅ macOS  
- ✅ Linux

Using Photino's lightweight Chromium wrapper - no Electron bloat, pure .NET performance.

---

## 🏥 What This Tool Does

Generates **algorithmically valid** medical credential numbers for testing:

### Supported Credentials

- **NPI Numbers** (National Provider Identifier)
  - 10 digits with Luhn check digit
  - Type 1 (individual) or Type 2 (organization)
  
- **DEA Numbers** (Drug Enforcement Administration)
  - Format: `[A-F][LastInitial][6-digits][checksum]`
  - Valid checksum algorithm
  
- **NDEA Numbers** (Narcotic Drug Enforcement Addiction)
  - Format: `[M/P][LastInitial][6-digits][checksum]`
  - For addiction treatment prescribers
  
- **Medical License Numbers**
  - State-specific formats (CA, NY, TX, FL, etc.)
  - Different formats for medical vs. pharmacy licenses

### ⚠️ Important

**For testing only.** Generated numbers are algorithmically valid but randomly generated - not registered with actual agencies.

---

## 🚀 Key Takeaways for BindSharp Users

### 1. **ROP Makes Complex Flows Simple**

Compare these two approaches to the same logic:

**Traditional:**
```csharp
public async Task<CreateNPIResponse> CreateNPI(bool isOrg)
{
    try
    {
        var npi = GenerateNPI(isOrg);
        
        try
        {
            var result = await _repo.AddAsync(npi);
            
            if (result == 0)
            {
                _logger.LogError("No rows inserted");
                throw new Exception("Insert failed");
            }
            
            return new CreateNPIResponse { NPI = npi.Value };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error");
            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create NPI");
        throw;
    }
}
```

**With BindSharp:**
```csharp
public async Task<Result<CreateNationalProviderIdentifierResponse, NationalProviderIdentifierError>>
    CreateNationalProviderIdentifier(bool isOrganization)
{
    var npiNumber = new NationalProviderIdentifierNumber
    {
        NationalProviderIdentifierNumberId = Guid.CreateVersion7(),
        NationalProviderIdentifier = CalculateLuhnCheckDigit(GenerateMiddleDigits(GetFirstDigit(isOrganization))),
        CreatedAt = DateTime.UtcNow
    };

    return await _nationalProviderIdentifierRepository
        .AddAsync(npiNumber)
        .MapAsync(_ => new CreateNationalProviderIdentifierResponse
        {
            NationalProviderIdentifier = npiNumber.NationalProviderIdentifier
        });
}
```

**54% less code. Infinitely more readable.**

---

### 2. **Type-Safe Errors Beat Magic Strings**

Instead of:
```csharp
throw new Exception("NPI must be exactly 10 digits");
```

Use domain-specific errors:
```csharp
new NationalProviderIdentifierValidationError("NPI must be exactly 10 digits")
```

Consumers can pattern match:
```csharp
result.Match(
    success => HandleSuccess(success),
    error => error switch
    {
        NationalProviderIdentifierValidationError validation => HandleValidation(validation),
        NationalProviderIdentifierInsertError database => HandleDatabase(database),
        _ => HandleUnknown(error)
    }
);
```

---

### 3. **Observability Comes Free**

Add logging without cluttering logic:
```csharp
ValidateMessage(message)
    .Tap(msg => _logger.LogDebug("Message validated: {0}", msg))
    .Bind(ParseMessage)
    .Tap(parsed => _logger.LogDebug("Parsed: {0}", parsed.Command))
    .TapError(error => _logger.LogWarning("Error: {0}", error))
    .Bind(ExecuteCommand);
```

Clean pipeline + comprehensive logging = maintainable code.

---

## 📖 Learning Path

**New to Railway-Oriented Programming?**

1. Start with the [BindSharp documentation](https://github.com/BindSharp/BindSharp)
2. Read the validation example: `NationalProviderIdentifierService.ValidateNpi`
3. Study the database layer: `NationalProviderIdentifierRepository.AddAsync`
4. Explore message routing: `MessageRouter.RouteMessage`
5. Build your own pipeline!

**Familiar with ROP?**

Check out these advanced patterns:
- Generic validation with `ValidateAffectedRows<TError>`
- Error type conversion with `MapError`
- Conditional processing with `BindIf`
- Multi-layer error handling

---

## 🤝 Contributing

This project exists to showcase BindSharp. Contributions welcome for:

- [ ] Additional BindSharp pattern examples
- [ ] Documentation improvements
- [ ] More credential types (NPI Type 2, etc.)
- [ ] Performance optimizations
- [ ] Additional state license formats

---

## 📄 License

GPL-3.0

---

## 🙏 Acknowledgments

**This project is a love letter to [BindSharp](https://github.com/BindSharp/BindSharp).**

BindSharp makes Railway-Oriented Programming accessible and practical for C# developers. This entire application is built on its foundation.

Special thanks to:
- **BindSharp** - For making error handling elegant
- **Photino** - For lightweight cross-platform desktop

---

## 🎯 The Bottom Line

**Railway-Oriented Programming with BindSharp transforms error handling from a chore into a design pattern.**

This project proves it works across:
- ✅ Complex validation (Luhn algorithm, checksums)
- ✅ Database operations (TryAsync, error conversion)
- ✅ Multi-step workflows (Bind chains)
- ✅ UI message routing (Tap/TapError observability)
- ✅ Cross-platform desktop applications

**Want to try BindSharp?** Visit [github.com/BindSharp/BindSharp](https://github.com/BindSharp/BindSharp)

---

**Built with 🚂 Railway-Oriented Programming**

*Because healthcare software deserves better error handling.*
