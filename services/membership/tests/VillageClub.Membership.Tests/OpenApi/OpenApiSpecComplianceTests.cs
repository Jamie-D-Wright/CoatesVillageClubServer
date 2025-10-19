using System.Net;
using FluentAssertions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VillageClub.Membership.Tests.OpenApi;

/// <summary>
/// Tests for OpenAPI specification compliance.
/// Validates that the YAML specification meets OpenAPI 3.0 standards.
/// </summary>
public class OpenApiSpecComplianceTests
{
    private readonly Dictionary<string, object>? _openApiSpec;
    
    // Use absolute path from workspace root
    private static string GetSpecFilePath()
    {
        // The test runs from: services/membership/tests/VillageClub.Membership.Tests/bin/Debug/net8.0
        // We need to navigate: bin -> Debug -> net8.0 -> VillageClub.Membership.Tests -> tests -> membership -> services -> workspace root
        var baseDir = AppContext.BaseDirectory; //  .../VillageClub.Membership.Tests/bin/Debug/net8.0
        var testProjectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..")); // .../VillageClub.Membership.Tests
        var membershipDir = Path.GetFullPath(Path.Combine(testProjectDir, "..", "..")); // .../services/membership
        var workspaceRoot = Path.GetFullPath(Path.Combine(membershipDir, "..", "..")); // workspace root
        return Path.Combine(workspaceRoot, "specs", "001-create-a-series", "contracts", "openapi", "membership-api.yaml");
    }

    public OpenApiSpecComplianceTests()
    {
        // Load and parse the OpenAPI YAML file
        var yamlContent = File.ReadAllText(GetSpecFilePath());
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        
        _openApiSpec = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
    }

    [Fact]
    public void OpenApiSpec_Should_Exist()
    {
        // Assert
        File.Exists(GetSpecFilePath()).Should().BeTrue("OpenAPI spec file should exist at the specified path");
    }

    [Fact]
    public void OpenApiSpec_Should_Have_Valid_OpenApi_Version()
    {
        // Arrange & Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("openapi");
        
        var version = _openApiSpec["openapi"].ToString();

        // Assert
        version.Should().StartWith("3.0", "OpenAPI version should be 3.0.x");
    }

    [Fact]
    public void OpenApiSpec_Should_Have_Info_Section()
    {
        // Arrange & Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("info");
        
        var info = _openApiSpec["info"] as Dictionary<object, object>;

        // Assert
        info.Should().NotBeNull();
        info!.Should().ContainKey("title");
        info.Should().ContainKey("description");
        info.Should().ContainKey("version");
        
        info["title"].ToString().Should().NotBeNullOrEmpty();
        info["version"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OpenApiSpec_Should_Have_Servers_Section()
    {
        // Arrange & Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("servers");
        
        var servers = _openApiSpec["servers"] as List<object>;

        // Assert
        servers.Should().NotBeNull();
        servers.Should().NotBeEmpty("At least one server should be defined");
        servers.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void OpenApiSpec_Should_Have_Paths_Section()
    {
        // Arrange & Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("paths");
        
        var paths = _openApiSpec["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        paths.Should().NotBeEmpty("API should define at least one path");
    }

    [Fact]
    public void OpenApiSpec_Should_Have_Components_Section()
    {
        // Arrange & Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("components");
        
        var components = _openApiSpec["components"] as Dictionary<object, object>;

        // Assert
        components.Should().NotBeNull();
        components.Should().ContainKey("schemas", "Components should define schemas for DTOs");
        components.Should().ContainKey("securitySchemes", "Components should define security schemes");
    }

    [Fact]
    public void OpenApiSpec_Should_Define_Authentication_Endpoints()
    {
        // Arrange
        var expectedAuthEndpoints = new[]
        {
            "/auth/login",
            "/auth/refresh",
            "/auth/logout"
        };

        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        foreach (var endpoint in expectedAuthEndpoints)
        {
            paths!.Keys.Cast<string>().Should().Contain(endpoint, $"Authentication endpoint {endpoint} should be defined");
        }
    }

    [Fact]
    public void OpenApiSpec_Should_Define_User_Management_Endpoints()
    {
        // Arrange
        var expectedUserEndpoints = new[]
        {
            "/users",
            "/users/{id}",
            "/users/me"
        };

        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        foreach (var endpoint in expectedUserEndpoints)
        {
            paths!.Keys.Cast<string>().Should().Contain(endpoint, $"User management endpoint {endpoint} should be defined");
        }
    }

    [Fact]
    public void OpenApiSpec_Should_Define_Health_Endpoint()
    {
        // Arrange
        var healthEndpoint = "/health";

        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        paths!.Keys.Cast<string>().Should().Contain(healthEndpoint, "Health check endpoint should be defined");
    }

    [Fact]
    public void OpenApiSpec_Should_Define_Security_Schemes()
    {
        // Act
        _openApiSpec.Should().NotBeNull();
        var components = _openApiSpec!["components"] as Dictionary<object, object>;
        var securitySchemes = components!["securitySchemes"] as Dictionary<object, object>;

        // Assert
        securitySchemes.Should().NotBeNull();
        securitySchemes.Should().ContainKey("BearerAuth", "JWT Bearer authentication should be defined");
    }

    [Fact]
    public void OpenApiSpec_Login_Endpoint_Should_Have_Required_Fields()
    {
        // Arrange
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;
        var loginPath = paths!["/auth/login"] as Dictionary<object, object>;
        var postOperation = loginPath!["post"] as Dictionary<object, object>;

        // Assert
        postOperation.Should().NotBeNull();
        postOperation!.Should().ContainKey("summary");
        postOperation.Should().ContainKey("operationId");
        postOperation.Should().ContainKey("requestBody");
        postOperation.Should().ContainKey("responses");
        
        var responses = postOperation["responses"] as Dictionary<object, object>;
        responses.Should().ContainKey("200", "Login should define 200 success response");
        responses.Should().ContainKey("401", "Login should define 401 unauthorized response");
        responses.Should().ContainKey("400", "Login should define 400 bad request response");
    }

    [Fact]
    public void OpenApiSpec_Should_Define_Common_Schemas()
    {
        // Arrange
        var expectedSchemas = new[]
        {
            "LoginRequest",
            "LoginResponse",
            "UserResponse",  // Changed from UserDto to match actual YAML schema name
            "ErrorResponse"
        };

        // Act
        _openApiSpec.Should().NotBeNull();
        var components = _openApiSpec!["components"] as Dictionary<object, object>;
        var schemas = components!["schemas"] as Dictionary<object, object>;

        // Assert
        schemas.Should().NotBeNull();
        foreach (var schemaName in expectedSchemas)
        {
            schemas!.Keys.Cast<string>().Should().Contain(schemaName, $"Schema {schemaName} should be defined");
        }
    }

    [Fact]
    public void OpenApiSpec_Protected_Endpoints_Should_Define_Security()
    {
        // Arrange - Protected endpoints that require authentication
        var protectedEndpoints = new List<(string Path, string Method)>
        {
            ("/users", "get"),
            ("/users/{id}", "get"),
            ("/users/{id}", "put"),
            ("/users/{id}", "delete"),
            ("/users/me", "get")
        };

        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        foreach (var endpoint in protectedEndpoints)
        {
            var path = paths![endpoint.Path] as Dictionary<object, object>;
            path.Should().NotBeNull($"Endpoint {endpoint.Path} should exist");
            
            var operation = path![endpoint.Method] as Dictionary<object, object>;
            operation.Should().NotBeNull($"Operation {endpoint.Method.ToUpper()} should exist for {endpoint.Path}");
            
            operation!.Should().ContainKey("security", $"Protected endpoint {endpoint.Method.ToUpper()} {endpoint.Path} should define security requirements");
        }
    }

    [Fact]
    public void OpenApiSpec_Should_Define_Tags()
    {
        // Arrange
        var expectedTags = new[]
        {
            "Authentication",
            "Users",
            "Health"
        };

        // Act
        _openApiSpec.Should().NotBeNull();
        _openApiSpec!.Should().ContainKey("tags");
        
        var tags = _openApiSpec["tags"] as List<object>;

        // Assert
        tags.Should().NotBeNull();
        tags.Should().HaveCountGreaterThan(expectedTags.Length - 1);
        
        var tagNames = tags!
            .Cast<Dictionary<object, object>>()
            .Select(t => t["name"].ToString())
            .ToList();
        
        foreach (var expectedTag in expectedTags)
        {
            tagNames.Should().Contain(expectedTag, $"Tag {expectedTag} should be defined");
        }
    }

    [Fact]
    public void OpenApiSpec_All_Endpoints_Should_Have_OperationIds()
    {
        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        
        foreach (var pathEntry in paths!)
        {
            var pathOperations = pathEntry.Value as Dictionary<object, object>;
            pathOperations.Should().NotBeNull();
            
            foreach (var operation in pathOperations!)
            {
                var operationDetails = operation.Value as Dictionary<object, object>;
                operationDetails.Should().NotBeNull();
                operationDetails!.Should().ContainKey("operationId", 
                    $"Operation {operation.Key} on path {pathEntry.Key} should have operationId");
            }
        }
    }

    [Fact]
    public void OpenApiSpec_All_Endpoints_Should_Have_Summary()
    {
        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        
        foreach (var pathEntry in paths!)
        {
            var pathOperations = pathEntry.Value as Dictionary<object, object>;
            pathOperations.Should().NotBeNull();
            
            foreach (var operation in pathOperations!)
            {
                var operationDetails = operation.Value as Dictionary<object, object>;
                operationDetails.Should().NotBeNull();
                operationDetails!.Should().ContainKey("summary", 
                    $"Operation {operation.Key} on path {pathEntry.Key} should have summary");
            }
        }
    }

    [Fact]
    public void OpenApiSpec_All_Endpoints_Should_Have_Responses()
    {
        // Act
        _openApiSpec.Should().NotBeNull();
        var paths = _openApiSpec!["paths"] as Dictionary<object, object>;

        // Assert
        paths.Should().NotBeNull();
        
        foreach (var pathEntry in paths!)
        {
            var pathOperations = pathEntry.Value as Dictionary<object, object>;
            pathOperations.Should().NotBeNull();
            
            foreach (var operation in pathOperations!)
            {
                var operationDetails = operation.Value as Dictionary<object, object>;
                operationDetails.Should().NotBeNull();
                operationDetails!.Should().ContainKey("responses", 
                    $"Operation {operation.Key} on path {pathEntry.Key} should define responses");
                
                var responses = operationDetails["responses"] as Dictionary<object, object>;
                responses.Should().NotBeNull();
                responses.Should().NotBeEmpty($"Operation {operation.Key} on path {pathEntry.Key} should have at least one response");
            }
        }
    }

    [Fact]
    public void OpenApiSpec_ErrorResponse_Schema_Should_Have_Required_Properties()
    {
        // Act
        _openApiSpec.Should().NotBeNull();
        var components = _openApiSpec!["components"] as Dictionary<object, object>;
        var schemas = components!["schemas"] as Dictionary<object, object>;
        var errorResponse = schemas!["ErrorResponse"] as Dictionary<object, object>;

        // Assert
        errorResponse.Should().NotBeNull();
        errorResponse.Should().ContainKey("properties");
        
        var properties = errorResponse!["properties"] as Dictionary<object, object>;
        properties.Should().NotBeNull();
        properties!.Should().ContainKey("message", "ErrorResponse should have message property");
        properties.Should().ContainKey("code", "ErrorResponse should have code property");
    }
}
