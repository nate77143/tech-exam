# Loan application pre-qualification

The original brief was to assess loan applications against Australian applicant and business rules and expose the result through a Web API with Swagger. This version uses .NET 8 minimal hosting and retains the controller endpoint.

## Pre-qualification rules

| Request field | Passing rule | Finding on failure |
| --- | --- | --- |
| `firstName`, `lastName` | At least one name is non-empty. | Unknown |
| `emailAddress` | Email format with a local part, one @, and a dotted domain. | Unknown |
| `phoneNumber` | Australian mobile (4) or landline (2, 3, 7, 8), preceded by 0 or +61 and followed by eight digits, with no separators. | Unknown |
| `businessNumber` | Exactly 11 ASCII digits. Subtract one from the first digit, multiply by weights 10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 in order, and require the sum to be divisible by 89. | Unqualified |
| `loanAmount` | Between the configured minimum and maximum, inclusive. Defaults are 10 and 100. | Unknown |
| `citizenshipStatus` | Exactly `Citizen` or `Permanent Resident`. | Unknown |
| `timeTrading` | Integer between the configured minimum and maximum, inclusive. Defaults are 1 and 20. | Unknown |
| `countryCode` | AU, case-insensitive. | Unknown |
| `industry` | Exact match in AllowedIndustries. BannedIndustries takes precedence. | Unqualified if banned, Unknown if unlisted |

The ranges and industry lists are in [appsettings.json](TechnicalTest/appsettings.json).

## Endpoint and decisions

Send a JSON object to `POST /LoanApplicationAssessment` with content type `application/json`. The API evaluates every rule and returns HTTP 200 for a completed assessment. A missing body or JSON `null` returns HTTP 400.

The overall `decision` is `Qualified` when every rule passes and `Unqualified` when any rule fails. Each entry in `validationResults` includes the rule name, a message, and its own decision. A finding of `Unqualified` identifies a disqualifying condition. A finding of `Unknown` identifies information that could not establish eligibility. An application with only `Unknown` findings still receives the original overall `Unqualified` decision.

This request qualifies with the default configuration.

```json
{
  "firstName": "Sam",
  "lastName": "Lee",
  "emailAddress": "sam.lee@example.com.au",
  "phoneNumber": "0412345678",
  "businessNumber": "51824753556",
  "loanAmount": 100,
  "citizenshipStatus": "Citizen",
  "timeTrading": 1,
  "countryCode": "au",
  "industry": "Industry 1"
}
```

The response is `{"decision":"Qualified","validationResults":[]}`.

## Run and test

From the repository root with a .NET 8 SDK, run the same checks as CI.

```sh
dotnet restore TechnicalTest.sln
dotnet build TechnicalTest.sln --configuration Release --no-restore
dotnet test TechnicalTest.sln --configuration Release --no-build
```

Start the API with `dotnet run --project TechnicalTest --launch-profile TechnicalTest` and open [Swagger UI](https://localhost:5001/swagger). The API simulates a one-second business-number lookup and caches assessments in memory with a ten-minute sliding expiration. The xUnit HTTP tests replace the delay with a completed task.
