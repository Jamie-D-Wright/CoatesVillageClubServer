using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace VillageClub.Membership.Tests.Functions;

/// <summary>
/// Base class for Azure Functions integration tests providing HTTP request mocking.
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
    }

    /// <summary>
    /// Override to configure services for the test.
    /// </summary>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Creates a mock HTTP request with the specified method, body, and query parameters.
    /// </summary>
    protected MockHttpRequestData CreateHttpRequest(
        string method = "GET",
        object? body = null,
        Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? headers = null)
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
            var json = JsonSerializer.Serialize(body);
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
        _url = new Uri("http://localhost:7071/api/test");
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    public MockHttpRequestData(FunctionContext functionContext, string method, Stream body)
        : base(functionContext)
    {
        Method = method;
        _url = new Uri("http://localhost:7071/api/test");
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

        // Update URL with query parameters
        var queryString = string.Join("&", _queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        _url = new Uri($"{_url.GetLeftPart(UriPartial.Path)}?{queryString}");
    }
}

/// <summary>
/// Mock implementation of HttpResponseData for testing with JSON serialization support.
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
    public async Task WriteJsonAsync<T>(T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
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
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override void Serialize(Stream stream, object? value, Type inputType, CancellationToken cancellationToken)
    {
        JsonSerializer.Serialize(stream, value, inputType, _options);
    }

    public override async ValueTask SerializeAsync(Stream stream, object? value, Type inputType, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(stream, value, inputType, _options, cancellationToken);
    }

    public override object? Deserialize(Stream stream, Type returnType, CancellationToken cancellationToken)
    {
        return JsonSerializer.Deserialize(stream, returnType, _options);
    }

    public override async ValueTask<object?> DeserializeAsync(Stream stream, Type returnType, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, returnType, _options, cancellationToken);
    }
}
