using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TechnicalTest.Tests;

public class AssessmentHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            services.AddSingleton<BusinessNumberDelay>(() => Task.CompletedTask));
    }
}

public class LoanAssessmentTests : IClassFixture<AssessmentHost>, IDisposable
{
    private readonly HttpClient client;
    private readonly AssessmentHost host;

    public LoanAssessmentTests(AssessmentHost host)
    {
        this.host = host;
        client = host.CreateClient();
    }

    public void Dispose() => client.Dispose();

    private static JsonObject Application() => new()
    {
        ["firstName"] = "Sam", ["lastName"] = "Lee",
        ["emailAddress"] = "sam.lee+loans@example.com.au",
        ["phoneNumber"] = "0412345678", ["businessNumber"] = "51824753556",
        ["loanAmount"] = 50, ["citizenshipStatus"] = "Citizen",
        ["timeTrading"] = 3, ["countryCode"] = "AU", ["industry"] = "Industry 1"
    };

    private async Task<JsonObject> Assess(JsonObject request)
    {
        using var response = await client.PostAsJsonAsync("/LoanApplicationAssessment", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    private static void AssertDecision(JsonObject result, string rule = null, string finding = null)
    {
        Assert.Equal(finding == null ? "Qualified" : "Unqualified", (string)result["decision"]);
        var findings = result["validationResults"].AsArray();
        if (finding == null)
        {
            Assert.Empty(findings);
            return;
        }
        var failure = Assert.Single(findings);
        Assert.Equal(rule, (string)failure["rule"]);
        Assert.Equal(finding, (string)failure["decision"]);
        Assert.False(string.IsNullOrWhiteSpace((string)failure["message"]));
    }

    [Fact]
    public async Task QualifiedApplicationReturnsEmptyFindings() =>
        AssertDecision(await Assess(Application()));

    [Theory]
    [InlineData("0412345678", true)]
    [InlineData("+61412345678", true)]
    [InlineData("0212345678", true)]
    [InlineData("+61312345678", true)]
    [InlineData("0712345678", true)]
    [InlineData("+61812345678", true)]
    [InlineData("0512345678", false)]
    [InlineData("+610412345678", false)]
    [InlineData("0412 345 678", false)]
    [InlineData("041234567", false)]
    [InlineData("04123456789", false)]
    [InlineData("0412345678\n", false)]
    [InlineData(null, false)]
    public async Task Phone(string value, bool valid) =>
        await Check("phoneNumber", value, "PhoneNumber", valid ? null : "Unknown");

    [Theory]
    [InlineData("51824753556", true)]
    [InlineData("53004085616", true)]
    [InlineData("51824753557", false)]
    [InlineData("00000000000", false)]
    [InlineData("5182475355A", false)]
    [InlineData("51 824 753 556", false)]
    [InlineData("5182475355", false)]
    [InlineData(null, false)]
    public async Task BusinessNumber(string value, bool valid) =>
        await Check("businessNumber", value, "BusinessNumber", valid ? null : "Unqualified");

    [Theory]
    [InlineData("Industry 1", null)]
    [InlineData("Industry 2", null)]
    [InlineData("Banned Industry 1", "Unqualified")]
    [InlineData("Other industry", "Unknown")]
    [InlineData(null, "Unknown")]
    public async Task Industry(string value, string finding) =>
        await Check("industry", value, "Industry", finding);

    [Theory]
    [InlineData("Citizen", true)]
    [InlineData("Permanent Resident", true)]
    [InlineData("citizen", false)]
    [InlineData("Visitor", false)]
    [InlineData(null, false)]
    public async Task Citizenship(string value, bool valid) =>
        await Check("citizenshipStatus", value, "CitizenshipStatus", valid ? null : "Unknown");

    [Theory]
    [InlineData("AU", true)]
    [InlineData("au", true)]
    [InlineData("Au", true)]
    [InlineData("NZ", false)]
    [InlineData(null, false)]
    public async Task Country(string value, bool valid) =>
        await Check("countryCode", value, "CountryCode", valid ? null : "Unknown");

    [Theory]
    [InlineData("sam+loans@example.com.au", true)]
    [InlineData("plain-text", false)]
    [InlineData("sam@@example.com", false)]
    [InlineData("a b@example.com", false)]
    [InlineData("sam@example", false)]
    [InlineData(null, false)]
    public async Task Email(string value, bool valid) =>
        await Check("emailAddress", value, "EmailAddress", valid ? null : "Unknown");

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(10000, true)]
    [InlineData(10001, false)]
    public async Task LoanAmount(int hundredths, bool valid) =>
        await Check("loanAmount", hundredths / 100m, "LoanAmount", valid ? null : "Unknown");

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public async Task TimeTrading(int value, bool valid) =>
        await Check("timeTrading", value, "TimeTrading", valid ? null : "Unknown");

    [Theory]
    [InlineData(null, "Lee", true)]
    [InlineData("Sam", "", true)]
    [InlineData("", null, false)]
    public async Task Name(string first, string last, bool valid)
    {
        var request = Application();
        request["firstName"] = first;
        request["lastName"] = last;
        AssertDecision(await Assess(request), "IsValidFirstNameLastName", valid ? null : "Unknown");
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public async Task MissingRequestReturnsBadRequest(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/LoanApplicationAssessment", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CacheKeepsAdjacentInputFieldsDistinct()
    {
        var request = Application();
        request["countryCode"] = "A";
        request["industry"] = "UIndustry 1";
        var failures = (await Assess(request))["validationResults"].AsArray();
        Assert.Equal(new[] { "CountryCode", "Industry" }, failures.Select(f => (string)f["rule"]));
        AssertDecision(await Assess(Application()));
    }

    [Fact]
    public async Task ServiceRejectsNullBeforeAccessingTheCache()
    {
        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILoanApplicationAssessmentService>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AssessLoanApplicationAsync(null));
    }

    private async Task Check(string field, JsonNode value, string rule, string finding)
    {
        var request = Application();
        request[field] = value;
        AssertDecision(await Assess(request), rule, finding);
    }
}
