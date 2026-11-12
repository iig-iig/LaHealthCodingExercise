# MembershipIntegration

## Overview

This solution models a small CRM-to-legacy membership integration built on Azure Functions v4 isolated worker. A registration JSON payload is accepted over HTTP, validated for required and format rules, transformed into the legacy membership API contract, and returned as a JSON response without calling the external legacy service during the exercise.

The objective is to keep the implementation small and maintainable while demonstrating clean separation of concerns:

- HTTP handling in the Azure Function
- validation in a dedicated service
- transformation in a dedicated mapper
- strongly typed domain and transport models
- dependency injection for service lifetime management

## Architecture

### HTTP Function
The function is responsible for boundary concerns only:

- accepting HTTP requests;
- reading the JSON body;
- handling empty/malformed request bodies;
- validating input via the validator service;
- mapping to the legacy contract;
- serializing the response;
- returning 200/400/500 as appropriate.

### Validation Service
`RegistrationValidator` ensures the CRM payload meets the contract before mapping. Its responsibilities are limited to:

- required field checks;
- format validation for `dateOfBirth` and `email`;
- membership type validation using exact case-sensitive matching.

It does not mutate input or perform mapping.

### Mapping Service
`RegistrationMapper` transforms a previously validated CRM model into the legacy JSON contract. It converts:

- `firstName` -> `member.given_name`
- `lastName` -> `member.family_name`
- `dateOfBirth` -> `member.dob`
- `email` -> `member.contact.email`
- `membershipType` -> `member.plan_code`
- source -> always `MILKYWAY`

It intentionally ignores `registeredAt` because the legacy contract does not support it.

### Models
The solution uses immutable record types for request/response models; these types separate transport concerns (`RegistrationRequest`) from transformation output (`LegacyMemberRequest`, `LegacyMember`). This keeps validation and mapping distinct and reduces accidental coupling.

### Dependency Injection
The host wires up the validator and mapper using constructor injection in `Program.cs`. This keeps the function focused on HTTP concerns while allowing unit tests to inject known dependencies quickly and deterministically.

### Why not a larger architecture?
For this exercise, a small layered pattern is better than introducing enterprise patterns like repositories, CQRS, MediatR, or domain events. The integration is a focused transformation with limited scope, and this approach keeps the code easier to reason about and review while still showing proper separation of concerns, dependency injection, and testability.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Visual Studio 2022 with the Azure development workload (optional, but recommended for local debugging)

## Build

```bash
dotnet restore
dotnet build
```

## Test

```bash
dotnet test
```

## Run locally

From the project folder:

```bash
cd MembershipIntegration.Functions
func start
```

## Example Request

```bash
curl -X POST "http://localhost:7071/api/registrations" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Alex",
    "lastName": "Nguyen",
    "dateOfBirth": "1990-04-12",
    "email": "alex.nguyen@example.com",
    "membershipType": "Single",
    "registeredAt": "2026-06-01T09:00:00Z"
  }'
```

## Successful Response

```json
{
  "member": {
    "given_name": "Alex",
    "family_name": "Nguyen",
    "dob": "12/04/1990",
    "contact": {
      "email": "alex.nguyen@example.com"
    },
    "plan_code": "S",
    "source": "MILKYWAY"
  }
}
```

## Validation Error

```json
{
  "errors": [
    "firstName is required",
    "email is invalid"
  ]
}
```

## Design Decisions

### DateOnly/TryParseExact
`dateOfBirth` is validated with `DateOnly.TryParseExact` using `yyyy-MM-dd` and `CultureInfo.InvariantCulture`. This prevents locale-specific issues and guarantees that only real calendar dates in the required format are accepted.

### Invariant culture
The solution uses `CultureInfo.InvariantCulture` for parsing and formatting dates so the same input is interpreted consistently regardless of the machine locale.

### Validation separation
Validation is intentionally isolated in `RegistrationValidator` so the function stays focused on HTTP orchestration and the mapper stays focused on transformation. This makes the logic easy to test independently and avoids duplicating rules across layers.

### Mapper separation
The mapper assumes input has already been validated. That keeps mapping deterministic and avoids mixing validation with transformation logic.

### System.Text.Json
The project uses `System.Text.Json` for both incoming request deserialization and outgoing JSON responses. This avoids unnecessary dependencies and matches the required .NET isolated worker patterns.

### Unknown-property handling
The serializer is configured to ignore unknown JSON properties and ignore case sensitivity on incoming property names. This means extra JSON data does not cause request rejection, as required by the exercise.

### Dependency injection
The validator and mapper are registered as singleton services in `Program.cs`. This is a lightweight and appropriate lifetime for stateless transformation and validation services.

### Validation error structure
Validation errors are serialized into a typed response using a consistent `errors` array and exact messages, which makes upstream clients easy to consume and matches the contract specification exactly.

## Production Outbound API Design

If this integration were extended to call the real legacy membership API, the next step would be introducing a small outbound client behind a typed `HttpClient` or an `IHttpClientFactory` configuration.

### HttpClient
Use `IHttpClientFactory` (or a typed `HttpClient`) instead of creating a new client for every request. Reusing clients helps with connection pooling, reduces socket churn, and is the standard pattern in ASP.NET Core and Azure Functions applications.

### Authentication
Realistic options include:

- OAuth 2.0 client credentials if the legacy API supports that model;
- API key authentication if the API is key-based;
- Azure Managed Identity when supported by the target service;
- Azure Key Vault for secret storage rather than hard-coding credentials.

The code should never contain secrets or plaintext credentials.

### Retry strategy
Use bounded exponential backoff with jitter for transient failures such as:

- HTTP 429
- HTTP 502
- HTTP 503
- HTTP 504
- transient network faults

Respect `Retry-After` when the API indicates it. Do not blindly retry normal 4xx validation or authentication failures, because those are usually deterministic and should be handled by business logic.

Modern .NET guidance typically uses `Microsoft.Extensions.Http.Resilience` or a similar resilience policy layer to centralize retry, timeout, and circuit-breaker behavior.

### Timeout strategy
Use sensible connection and request timeouts with cancellation tokens so requests cannot hang indefinitely. This should be bounded and explicit, particularly when calling downstream systems in production.

### Logging and monitoring
This solution is compatible with Azure Application Insights, structured `ILogger` logging, and correlation IDs. In production, the team would monitor:

- failure rate;
- latency;
- retry counts;
- dependency telemetry;
- alerts on sustained errors.

Sensitive member information should never be logged unnecessarily.

### Idempotency
If the outbound API call is retried after a transient failure, the integration should guard against accidental duplicate membership creation. Typical approaches include:

- idempotency keys sent with the request;
- de-duplication checks using unique member identifiers or external correlation IDs;
- storing request metadata to detect a previous successful outbound call.

## Solution creation commands

```bash
mkdir MembershipIntegration
cd MembershipIntegration

dotnet new sln -n MembershipIntegration

dotnet new xunit -n MembershipIntegration.Tests -o MembershipIntegration.Tests
dotnet new classlib -n MembershipIntegration.Functions -o MembershipIntegration.Functions --framework net8.0

dotnet sln MembershipIntegration.sln add MembershipIntegration.Functions/MembershipIntegration.Functions.csproj MembershipIntegration.Tests/MembershipIntegration.Tests.csproj

dotnet add MembershipIntegration.Tests/MembershipIntegration.Tests.csproj reference MembershipIntegration.Functions/MembershipIntegration.Functions.csproj

dotnet restore
dotnet build
dotnet test
```

For Azure Functions isolated worker, the project should then be updated to add the required worker package references and function code.

## Run the function locally

```bash
cd MembershipIntegration.Functions
func start
```
