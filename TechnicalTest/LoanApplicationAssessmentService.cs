using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TechnicalTest
{
    public class LoanApplicationAssessmentService : ILoanApplicationAssessmentService
    {
        private static readonly Regex EmailPattern = new(
            @"\A[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\z",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        private static readonly Regex PhonePattern = new(
            @"\A(?:\+61|0)[23478][0-9]{8}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly int[] AbnWeights = { 10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
        private readonly BusinessNumberDelay _businessNumberDelay;
        private readonly int _minLoanAmount;
        private readonly int _maxLoanAmount;
        private readonly int _minTimeTrading;
        private readonly int _maxTimeTrading;
        private readonly List<string> _allowedIndustries;
        private readonly List<string> _bannedIndustries;

        private readonly IMemoryCache _cache;
        public LoanApplicationAssessmentService(IMemoryCache memoryCache, IConfiguration configuration, BusinessNumberDelay businessNumberDelay)
        {
            _allowedIndustries = configuration.GetSection("AllowedIndustries").Get<List<string>>() ?? new List<string>();
            _bannedIndustries = configuration.GetSection("BannedIndustries").Get<List<string>>() ?? new List<string>();
            _minLoanAmount = configuration.GetSection("MinLoanAmount").Get<int>();
            _maxLoanAmount = configuration.GetSection("MaxLoanAmount").Get<int>();
            _minTimeTrading = configuration.GetSection("MinTimeTrading").Get<int>();
            _maxTimeTrading = configuration.GetSection("MaxTimeTrading").Get<int>();
            _cache = memoryCache;
            _businessNumberDelay = businessNumberDelay;
        }

        public async Task<LoanAssessmentResult> AssessLoanApplicationAsync(LoanApplicationAssessmentRequest loanApplication)
        {
            ArgumentNullException.ThrowIfNull(loanApplication);
            var uid = GetUniqueIdentifier(loanApplication);
            if (_cache.TryGetValue(uid, out LoanAssessmentResult cachedResult))
            {
                return cachedResult;
            }

            var validationResult = new List<ValidationResult>();
            var decision = Decision.Unqualified;

            if (!IsValidFirstNameLastName(loanApplication))
            {
                validationResult.Add(new ValidationResult(nameof(IsValidFirstNameLastName),
                    "Invalid first name or last name", Decision.Unknown.ToString()));
            }

            if (!IsValidEmail(loanApplication.EmailAddress))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.EmailAddress), 
                    "Invalid Email Address", Decision.Unknown.ToString()));
            }

            if (!IsValidPhoneNumber(loanApplication.PhoneNumber))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.PhoneNumber),
                    "Must be a valid Australian phone number format", Decision.Unknown.ToString()));
            }

            if (!await IsValidBusinessNumberAsync(loanApplication.BusinessNumber))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.BusinessNumber),
                    "Incorrect Business Number", Decision.Unqualified.ToString()));
            }

            if (!IsValidLoanAmount(loanApplication.LoanAmount))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.LoanAmount),
                    "Loan amount is invalid", Decision.Unknown.ToString()));
            }

            if (!IsValidCitizenshipStatus(loanApplication.CitizenshipStatus))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.CitizenshipStatus),
                    "Citizenship status is not provided or is not of the known values", Decision.Unknown.ToString()));
            }

            if (!IsValidTimeTrading(loanApplication.TimeTrading))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.TimeTrading),
                    "Time trading is not provided or is not within valid range", Decision.Unknown.ToString()));
            }

            if (!IsValidCountryCode(loanApplication.CountryCode))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.CountryCode),
                    "Invalid country code", Decision.Unknown.ToString()));
            }

            if (_bannedIndustries.Contains(loanApplication.Industry))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.Industry),
                    "Industry is banned", Decision.Unqualified.ToString()));
            }
            else if (!_allowedIndustries.Contains(loanApplication.Industry))
            {
                validationResult.Add(new ValidationResult(nameof(loanApplication.Industry),
                    "Industry is not listed", Decision.Unknown.ToString()));
            }

            if (validationResult.Count == 0)
            {
                decision = Decision.Qualified;
            }

            var loanAssessmentResult = new LoanAssessmentResult
            {
                Decision = decision.ToString(),
                validationResults = validationResult
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10)); // Cache for 10 minutes
            _cache.Set(uid, loanAssessmentResult, cacheOptions);

            return loanAssessmentResult;

        }

        private static bool IsValidFirstNameLastName(LoanApplicationAssessmentRequest lead)
        {
            return (!string.IsNullOrEmpty(lead.FirstName) || !string.IsNullOrEmpty(lead.LastName));  
        }
        private static bool IsValidEmail(string email) =>
            !string.IsNullOrEmpty(email) && EmailPattern.IsMatch(email);

        private static bool IsValidPhoneNumber(string phoneNumber) =>
            !string.IsNullOrEmpty(phoneNumber) && PhonePattern.IsMatch(phoneNumber);

        private async Task<bool> IsValidBusinessNumberAsync(string businessNumber)
        {
            if (businessNumber?.Length != AbnWeights.Length || !businessNumber.All(char.IsAsciiDigit))
            {
                return false;
            }

            await _businessNumberDelay();
            int sum = -AbnWeights[0]; // Subtract one from the first digit before weighting.
            for (int i = 0; i < AbnWeights.Length; i++)
            {
                sum += (businessNumber[i] - '0') * AbnWeights[i];
            }

            return sum % 89 == 0;
        }

        private bool IsValidLoanAmount(decimal loanAmount)
        {
            return loanAmount >= _minLoanAmount && loanAmount <= _maxLoanAmount;
        }
        private bool IsValidCitizenshipStatus(string citizenshipStatus)
        {
            return citizenshipStatus == "Citizen" || citizenshipStatus == "Permanent Resident";
        }
        private bool IsValidTimeTrading(int timeTrading)
        {
            return timeTrading >= _minTimeTrading && timeTrading <= _maxTimeTrading;
        }
        private static bool IsValidCountryCode(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                return false;
            }
            return countryCode.Equals("AU", StringComparison.OrdinalIgnoreCase);
        }
        private static string GetUniqueIdentifier(LoanApplicationAssessmentRequest loanApplication)
        {
            var fields = new[]
            {
                loanApplication.FirstName, loanApplication.LastName, loanApplication.EmailAddress,
                loanApplication.PhoneNumber, loanApplication.BusinessNumber,
                loanApplication.LoanAmount.ToString(CultureInfo.InvariantCulture),
                loanApplication.CitizenshipStatus, loanApplication.TimeTrading.ToString(CultureInfo.InvariantCulture),
                loanApplication.CountryCode, loanApplication.Industry
            };
            return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(fields)));
        }
    }
}
