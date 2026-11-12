using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MembershipIntegration.Functions.Functions;
using MembershipIntegration.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MembershipIntegration.Tests;

public class RegistrationFunctionTests
{
    [Fact]
    public async Task ValidRequestReturnsHttp200()
    {
        var function = CreateFunction();
        var request = CreateRequest("{\n  \"firstName\": \"Alex\",\n  \"lastName\": \"Nguyen\",\n  \"dateOfBirth\": \"1990-04-12\",\n  \"email\": \"alex.nguyen@example.com\",\n  \"membershipType\": \"Single\"\n}");

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadBodyAsync(response);
        json.Should().Contain("\"member\"");
        json.Should().Contain("\"given_name\":\"Alex\"");
        json.Should().Contain("\"plan_code\":\"S\"");
    }

    [Fact]
    public async Task ValidationFailureReturnsHttp400()
    {
        var function = CreateFunction();
        var request = CreateRequest("{\"firstName\":\"\",\"lastName\":\"Nguyen\",\"dateOfBirth\":\"1990-04-12\",\"email\":\"alex.nguyen@example.com\",\"membershipType\":\"Single\"}");

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await ReadBodyAsync(response);
        json.Should().Contain("firstName is required");
    }

    [Fact]
    public async Task MultipleValidationFailuresAreReturned()
    {
        var function = CreateFunction();
        var request = CreateRequest("{\"firstName\":\"\",\"lastName\":\"\",\"dateOfBirth\":\"1990-04-12\",\"email\":\"not-an-email\",\"membershipType\":\"invalid\"}");

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await ReadBodyAsync(response);
        json.Should().Contain("firstName is required");
        json.Should().Contain("lastName is required");
        json.Should().Contain("email is invalid");
        json.Should().Contain("membershipType must be Single, Family or Couple");
    }

    [Fact]
    public async Task MalformedJsonReturnsHttp400()
    {
        var function = CreateFunction();
        var request = CreateRequest("{ \"firstName\": ");

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await ReadBodyAsync(response);
        json.Should().Contain("Request body contains invalid JSON");
    }

    [Fact]
    public async Task EmptyBodyReturnsHttp400()
    {
        var function = CreateFunction();
        var request = CreateRequest(string.Empty);

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await ReadBodyAsync(response);
        json.Should().Contain("Request body is required");
    }

    [Fact]
    public async Task UnexpectedAdditionalJsonFieldsAreIgnored()
    {
        var function = CreateFunction();
        var request = CreateRequest("{\"firstName\":\"Alex\",\"lastName\":\"Nguyen\",\"dateOfBirth\":\"1990-04-12\",\"email\":\"alex.nguyen@example.com\",\"membershipType\":\"Single\",\"extraProperty\":\"ignored\"}");

        var response = await function.Run(request, new TestFunctionContext(), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadBodyAsync(response);
        json.Should().NotContain("extraProperty");
        json.Should().Contain("\"source\":\"MILKYWAY\"");
    }

    private static RegistrationFunction CreateFunction()
    {
        return new RegistrationFunction(new RegistrationValidator(), new RegistrationMapper(), NullLogger<RegistrationFunction>.Instance);
    }

    private static TestHttpRequestData CreateRequest(string json)
    {
        return new TestHttpRequestData(new TestFunctionContext(), json);
    }

    private static async Task<string> ReadBodyAsync(HttpResponseData response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}

public sealed class TestFunctionContext : FunctionContext
{
    public override string InvocationId => "test-invocation";

    public override string FunctionId => "test-function";

    public override TraceContext TraceContext { get; } = new TestTraceContext();

    public override BindingContext BindingContext { get; } = new TestBindingContext();

    public override RetryContext RetryContext { get; } = new TestRetryContext();

    public override IServiceProvider InstanceServices { get; set; } = new ServiceCollection().BuildServiceProvider();

    public override FunctionDefinition FunctionDefinition { get; } = new TestFunctionDefinition();

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IInvocationFeatures Features { get; } = new TestInvocationFeatures();

    public override CancellationToken CancellationToken => CancellationToken.None;
}

public sealed class TestTraceContext : TraceContext
{
    public override string? TraceParent => "test-parent";

    public override string? TraceState => string.Empty;
}

public sealed class TestBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object> BindingData { get; } = new Dictionary<string, object>();
}

public sealed class TestRetryContext : RetryContext
{
    public override int RetryCount => 0;

    public override int MaxRetryCount => 0;
}

public sealed class TestFunctionDefinition : FunctionDefinition
{
    public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;

    public override string PathToAssembly => string.Empty;

    public override string EntryPoint => "MembershipIntegration.Functions.Functions.RegistrationFunction.Run";

    public override string Id => "registration-function";

    public override string Name => "RegistrationFunction";

    public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;

    public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
}

public sealed class TestInvocationFeatures : IInvocationFeatures
{
    private readonly Dictionary<Type, object> _features = new();

    public void Set<T>(T instance)
    {
        _features[typeof(T)] = instance!;
    }

    public T Get<T>()
    {
        if (_features.TryGetValue(typeof(T), out var value))
        {
            return (T)value;
        }

        throw new InvalidOperationException($"Feature type {typeof(T).FullName} is not registered.");
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        return _features.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class TestHttpRequestData : HttpRequestData
{
    private readonly MemoryStream _body;

    public TestHttpRequestData(FunctionContext functionContext, string body)
        : base(functionContext)
    {
        _body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? string.Empty));
        Headers = new HttpHeadersCollection();
        Cookies = new List<IHttpCookie>();
        Url = new Uri("https://localhost/api/registrations");
        Identities = Enumerable.Empty<ClaimsIdentity>();
        Method = "POST";
        Query = new NameValueCollection();
    }

    public override Stream Body => _body;

    public override HttpHeadersCollection Headers { get; }

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities { get; }

    public override string Method { get; }

    public override NameValueCollection Query { get; }

    public override HttpResponseData CreateResponse()
    {
        return new TestHttpResponseData(FunctionContext);
    }
}

public sealed class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Cookies = new TestHttpCookies();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; }

    public override Stream Body { get; set; }

    public override HttpCookies Cookies { get; }
}

public sealed class TestHttpCookies : HttpCookies
{
    private readonly List<IHttpCookie> _cookies = new();

    public override void Append(string name, string value)
    {
        _cookies.Add(new TestHttpCookie { Name = name, Value = value });
    }

    public override void Append(IHttpCookie cookie)
    {
        _cookies.Add(cookie);
    }

    public override IHttpCookie CreateNew()
    {
        return new TestHttpCookie();
    }
}

public sealed class TestHttpCookie : IHttpCookie
{
    public string Domain { get; set; } = string.Empty;

    public DateTimeOffset? Expires { get; set; }

    public bool? HttpOnly { get; set; }

    public double? MaxAge { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public SameSite SameSite { get; set; }

    public bool? Secure { get; set; }

    public string Value { get; set; } = string.Empty;
}
