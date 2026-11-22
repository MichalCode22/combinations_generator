using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using CombinationsGenerator.Application.SessionStore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CombinationsGenerator.Api.Tests;

public sealed class CombinationsApiTests
{
    [Fact]
    public async Task Endpoints_support_start_next_all_current_and_reset()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var start = await PostJsonAsync(client, "/api/combinations/start", "{\"n\":3}");
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var startJson = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        Assert.Equal("6", startJson.RootElement.GetProperty("total").GetString());
        var sessionId = startJson.RootElement.GetProperty("sessionId").GetGuid();

        using var next = await SendSessionRequestAsync(client, HttpMethod.Post, "/api/combinations/next", sessionId);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        using var nextJson = JsonDocument.Parse(await next.Content.ReadAsStringAsync());
        Assert.Equal("1", nextJson.RootElement.GetProperty("index").GetString());
        Assert.Equal(new[] { 1, 2, 3 }, nextJson.RootElement.GetProperty("permutation").EnumerateArray()
            .Select(value => value.GetInt32()).ToArray());

        using var current = await SendSessionRequestAsync(client, HttpMethod.Get, "/api/combinations/current", sessionId);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        using var currentJson = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        Assert.Equal("1", currentJson.RootElement.GetProperty("index").GetString());

        using var all = await SendSessionRequestAsync(
            client,
            HttpMethod.Get,
            "/api/combinations/all?pageSize=2&page=1&enter=true",
            sessionId);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        using var allJson = JsonDocument.Parse(await all.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "2", "3" }, allJson.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("index").GetString()).ToArray());

        using var reset = await SendSessionRequestAsync(client, HttpMethod.Delete, "/api/combinations", sessionId);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        using var missing = await SendSessionRequestAsync(client, HttpMethod.Get, "/api/combinations/current", sessionId);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("{\"n\":0}")]
    [InlineData("{\"n\":21}")]
    [InlineData("{\"n\":-1}")]
    [InlineData("{\"n\":3.5}")]
    [InlineData("{\"n\":\"abc\"}")]
    [InlineData("{\"n\":null}")]
    [InlineData("{\"n\":999999999999999999999999999999}")]
    public async Task Start_rejects_invalid_n_with_problem_details(string body)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostJsonAsync(client, "/api/combinations/start", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_rejects_missing_body()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/combinations/start",
            new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public async Task All_rejects_invalid_page_size(int pageSize)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await StartSessionAsync(client, 3);

        using var response = await SendSessionRequestAsync(
            client,
            HttpMethod.Get,
            $"/api/combinations/all?pageSize={pageSize}",
            sessionId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("page=0&pageSize=2")]
    [InlineData("page=4&pageSize=2")]
    [InlineData("page=abc")]
    [InlineData("page=18446744073709551616")]
    public async Task All_rejects_invalid_page(string query)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await StartSessionAsync(client, 3);

        using var response = await SendSessionRequestAsync(
            client,
            HttpMethod.Get,
            $"/api/combinations/all?{query}",
            sessionId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Session_endpoints_return_404_for_missing_invalid_and_expired_ids()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var missing = await client.GetAsync("/api/combinations/current");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemDetailsAsync(missing, HttpStatusCode.NotFound);

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/combinations/current");
        invalidRequest.Headers.Add("X-Session-Id", "not-a-guid");
        using var invalid = await client.SendAsync(invalidRequest);
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);

        var expiredSession = await StartSessionAsync(client, 2);
        factory.Clock.Advance(TimeSpan.FromMinutes(31));
        using var expired = await SendSessionRequestAsync(
            client,
            HttpMethod.Get,
            "/api/combinations/current",
            expiredSession);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        await AssertProblemDetailsAsync(expired, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Large_counters_are_serialized_as_decimal_strings_and_last_page_is_reachable()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var start = await PostJsonAsync(client, "/api/combinations/start", "{\"n\":20}");
        start.EnsureSuccessStatusCode();
        var startBody = await start.Content.ReadAsStringAsync();
        using var startJson = JsonDocument.Parse(startBody);
        var sessionId = startJson.RootElement.GetProperty("sessionId").GetGuid();

        Assert.Equal(JsonValueKind.String, startJson.RootElement.GetProperty("total").ValueKind);
        Assert.Equal("2432902008176640000", startJson.RootElement.GetProperty("total").GetString());
        var total = ulong.Parse(startJson.RootElement.GetProperty("total").GetString()!, CultureInfo.InvariantCulture);
        const int pageSize = 100;
        var lastPage = (total - 1) / pageSize + 1;

        using var response = await SendSessionRequestAsync(
            client,
            HttpMethod.Get,
            $"/api/combinations/all?pageSize={pageSize}&page={lastPage}&enter=true",
            sessionId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("page").ValueKind);
        Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("totalPages").ValueKind);
        Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("startIndex").ValueKind);
        Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("total").ValueKind);
        Assert.Equal(lastPage.ToString(CultureInfo.InvariantCulture), json.RootElement.GetProperty("page").GetString());

        var lastItem = json.RootElement.GetProperty("items").EnumerateArray().Last();
        Assert.Equal(JsonValueKind.String, lastItem.GetProperty("index").ValueKind);
        Assert.Equal(total.ToString(CultureInfo.InvariantCulture), lastItem.GetProperty("index").GetString());
        Assert.Equal(Enumerable.Range(1, 20).Reverse(),
            lastItem.GetProperty("permutation").EnumerateArray().Select(value => value.GetInt32()));
    }

    [Fact]
    public async Task Cors_allows_the_angular_origin()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/combinations/current");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "X-Session-Id");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("x-session-id",
            response.Headers.GetValues("Access-Control-Allow-Headers").Single(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Guid> StartSessionAsync(HttpClient client, int n)
    {
        using var response = await PostJsonAsync(client, "/api/combinations/start", $"{{\"n\":{n}}}");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("sessionId").GetGuid();
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string body) =>
        client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));

    private static async Task<HttpResponseMessage> SendSessionRequestAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        Guid sessionId)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Session-Id", sessionId.ToString());
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = json.RootElement;
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.True(problem.TryGetProperty("type", out _));
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public AdjustableTimeProvider Clock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(Clock));
        }
    }

    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now += amount;
    }
}
