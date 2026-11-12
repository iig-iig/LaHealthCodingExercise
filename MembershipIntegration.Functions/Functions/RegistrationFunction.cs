using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using MembershipIntegration.Functions.Models;
using MembershipIntegration.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MembershipIntegration.Functions.Functions;

public sealed class RegistrationFunction
{
    private const string InvalidJsonMessage = "Request body contains invalid JSON";
    private const string MissingBodyMessage = "Request body is required";
    private const string UnexpectedErrorMessage = "An unexpected error occurred";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IRegistrationValidator _validator;
    private readonly IRegistrationMapper _mapper;
    private readonly ILogger<RegistrationFunction> _logger;

    public RegistrationFunction(
        IRegistrationValidator validator,
        IRegistrationMapper mapper,
        ILogger<RegistrationFunction> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("RegistrationFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "registrations")] HttpRequestData request,
        FunctionContext executionContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration request received.");

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string body;
        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to read the request body.");
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, new ValidationErrorResponse(new[] { MissingBodyMessage }), cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogWarning("Empty request body received.");
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, new ValidationErrorResponse(new[] { MissingBodyMessage }), cancellationToken);
        }

        RegistrationRequest? registration;

        try
        {
            registration = JsonSerializer.Deserialize<RegistrationRequest>(body, SerializerOptions);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Malformed request body received.");
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, new ValidationErrorResponse(new[] { InvalidJsonMessage }), cancellationToken);
        }

        if (registration is null)
        {
            _logger.LogWarning("Request body could not be deserialized to RegistrationRequest.");
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, new ValidationErrorResponse(new[] { InvalidJsonMessage }), cancellationToken);
        }

        var validationErrors = _validator.Validate(registration);
        if (validationErrors.Errors.Count > 0)
        {
            _logger.LogWarning("Validation failed with {Count} error(s).", validationErrors.Errors.Count);
            return await CreateJsonResponseAsync(request, HttpStatusCode.BadRequest, validationErrors, cancellationToken);
        }

        var legacyRequest = _mapper.Map(registration);
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(legacyRequest, SerializerOptions)));

        _logger.LogInformation("Registration transformed successfully.");

        return response;
    }

    private static Task<HttpResponseData> CreateJsonResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        object payload,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions)));
        return Task.FromResult(response);
    }
}
