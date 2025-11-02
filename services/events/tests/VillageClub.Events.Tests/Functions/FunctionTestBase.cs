using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace VillageClub.Events.Tests.Functions;

/// <summary>
/// Base class for Azure Functions integration tests providing HTTP request mocking and test infrastructure.
/// </summary>
public abstract class FunctionTestBase : IDisposable
{
    protected readonly Mock<FunctionContext> MockFunctionContext;
    protected readonly IServiceProvider ServiceProvider;
    private bool _disposed;

    protected FunctionTestBase()
    {
        var services = new ServiceCollection();
        
        // Add Azure Functions worker serializer (required for WriteAsJsonAsync)
        services.Configure<Microsoft.Azure.Functions.Worker.WorkerOptions>(options =>
        {
            options.Serializer = new TestJsonObjectSerializer();
        });
        
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        MockFunctionContext = new Mock<FunctionContext>();
        MockFunctionContext.Setup(ctx => ctx.InstanceServices)
            .Returns(ServiceProvider);
        
        // Initialize Items to empty dictionary for unauthenticated scenarios
        MockFunctionContext.Setup(ctx => ctx.Items)
            .Returns(new Dictionary<object, object>());
    }

    /// <summary>
    /// Override to configure services for the test.
    /// </summary>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Creates a mock HTTP request with the specified method, URL (with query string), and body.
    /// </summary>
    protected MockHttpRequestData CreateHttpRequest(
        string method = "GET",
        string? url = null,
        object? body = null,
        Dictionary<string, string>? headers = null)
    {
        // Parse query parameters from URL if provided
        Dictionary<string, string>? queryParams = null;
        if (!string.IsNullOrEmpty(url) && url.Contains('?'))
        {
            var parts = url.Split('?', 2);
            var queryString = parts[1];
            queryParams = System.Web.HttpUtility.ParseQueryString(queryString)
                .AllKeys
                .Where(k => k != null)
                .ToDictionary(k => k!, k => System.Web.HttpUtility.ParseQueryString(queryString)[k]!);
        }

        return CreateHttpRequestCore(method, queryParams, body, headers);
    }

    /// <summary>
    /// Creates a mock HTTP request with the specified method, body, and query parameters.
    /// This overload is for cases where body is the primary concern (POST, PUT, etc.).
    /// </summary>
    protected MockHttpRequestData CreateHttpRequest(
        string method,
        object body,
        Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? headers = null)
    {
        return CreateHttpRequestCore(method, queryParams, body, headers);
    }

    /// <summary>
    /// Core logic for creating mock HTTP requests.
    /// </summary>
    private MockHttpRequestData CreateHttpRequestCore(
        string method,
        Dictionary<string, string>? queryParams,
        object? body,
        Dictionary<string, string>? headers)
    {
        var mockRequest = new MockHttpRequestData(MockFunctionContext.Object, method);

        // Add query parameters
        if (queryParams != null)
        {
            foreach (var (key, value) in queryParams)
            {
                mockRequest.AddQueryParameter(key, value);
            }
        }

        // Add headers
        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                mockRequest.Headers.Add(key, value);
            }
        }

        // Serialize body if provided
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(bytes);
            mockRequest = new MockHttpRequestData(MockFunctionContext.Object, method, stream);

            // Re-add query parameters and headers to new request
            if (queryParams != null)
            {
                foreach (var (key, value) in queryParams)
                {
                    mockRequest.AddQueryParameter(key, value);
                }
            }

            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    mockRequest.Headers.Add(key, value);
                }
            }
        }

        return mockRequest;
    }

    /// <summary>
    /// Reads the response body as a deserialized object.
    /// </summary>
    protected async Task<T?> ReadResponseBody<T>(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        });
    }

    /// <summary>
    /// Reads the response body as a string.
    /// </summary>
    protected async Task<string> ReadResponseBodyAsString(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Sets up authentication context for testing.
    /// </summary>
    protected void SetupAuthContext(Guid userId, string email, string role, string? committeeRole = null)
    {
        var items = new Dictionary<object, object>
        {
            ["UserId"] = userId,
            ["Email"] = email,
            ["Role"] = role,
        };

        if (committeeRole != null)
        {
            items["CommitteeRole"] = committeeRole;
        }

        MockFunctionContext.Setup(ctx => ctx.Items).Returns(items);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing && ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Mock implementation of HttpRequestData for testing.
/// </summary>
public class MockHttpRequestData : HttpRequestData
{
    private readonly Dictionary<string, string> _queryParams = new();
    private Uri _url;

    public MockHttpRequestData(FunctionContext functionContext, string method = "GET")
        : base(functionContext)
    {
        Method = method;
        _url = new Uri("http://localhost:7072/api/v1/test");
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public MockHttpRequestData(FunctionContext functionContext, string method, Stream body)
        : base(functionContext)
    {
        Method = method;
        _url = new Uri("http://localhost:7072/api/v1/test");
        Headers = new HttpHeadersCollection();
        Body = body;
    }

    public override Stream Body { get; }

    public override HttpHeadersCollection Headers { get; }

    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

    public override Uri Url => _url;

    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();

    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        return new MockHttpResponseData(FunctionContext);
    }

    public void AddQueryParameter(string key, string value)
    {
        _queryParams[key] = value;
        
        var queryString = string.Join("&", _queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        _url = new Uri($"{_url.GetLeftPart(UriPartial.Path)}?{queryString}");
    }
}

/// <summary>
/// Mock implementation of HttpResponseData for testing.
/// </summary>
public class MockHttpResponseData : HttpResponseData
{
    public MockHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    {
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; }

    public override Stream Body { get; set; }

    public override HttpCookies Cookies => throw new NotImplementedException();

    /// <summary>
    /// Helper method to write JSON to the response body.
    /// </summary>
    /// <typeparam name="T">Type of value to serialize.</typeparam>
    /// <param name="value">Value to write.</param>
    public async Task WriteJsonAsync<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        });
        
        await using var writer = new StreamWriter(Body, leaveOpen: true);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        Body.Position = 0;
    }
}

/// <summary>
/// Simple JSON object serializer for testing that extends Azure.Core.Serialization.ObjectSerializer.
/// </summary>
internal class TestJsonObjectSerializer : Azure.Core.Serialization.ObjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override void Serialize(Stream stream, object? value, Type inputType, CancellationToken cancellationToken)
    {
        JsonSerializer.Serialize(stream, value, inputType, Options);
    }

    public override async ValueTask SerializeAsync(Stream stream, object? value, Type inputType, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(stream, value, inputType, Options, cancellationToken);
    }

    public override object? Deserialize(Stream stream, Type returnType, CancellationToken cancellationToken)
    {
        return JsonSerializer.Deserialize(stream, returnType, Options);
    }

    public override async ValueTask<object?> DeserializeAsync(Stream stream, Type returnType, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, returnType, Options, cancellationToken);
    }
}
