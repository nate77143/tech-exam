using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Technical_Test_Nathaniel_Conwi
{
    public class LoanApplicationAssessmentService : ILoanApplicationAssessmentService
    {
        private readonly int _minLoanAmount;
        private readonly int _maxLoanAmount;
        private readonly int _minTimeTrading;
        private readonly int _maxTimeTrading;
        private readonly List<string> _allowedIndustries;
        private readonly List<string> _bannedIndustries;

        private readonly IMemoryCache _cache;
        public LoanApplicationAssessmentService(IMemoryCache memoryCache, IConfiguration configuration)
        {
            _allowedIndustries = configuration.GetSection("AllowedIndustries").Get<List<string>>();
            _bannedIndustries = configuration.GetSection("BannedIndustries").Get<List<string>>();
            _minLoanAmount = configuration.GetSection("MinLoanAmount").Get<int>();
            _maxLoanAmount = configuration.GetSection("MaxLoanAmount").Get<int>();
            _minTimeTrading = configuration.GetSection("MinTimeTrading").Get<int>();
            _maxTimeTrading = configuration.GetSection("MaxTimeTrading").Get<int>();
            _cache = memoryCache;
        }

        public async Task<LoanAssessmentResult> AssessLoanApplicationAsync(LoanApplicationAssessmentRequest loanApplication)
        {
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

        private bool IsValidFirstNameLastName(LoanApplicationAssessmentRequest lead)
        {
            return (!string.IsNullOrEmpty(lead.FirstName) || !string.IsNullOrEmpty(lead.LastName));  
        }
        private bool IsValidEmail(string email)
        {
            return !string.IsNullOrEmpty(email);
        }
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return false;
            }

            string pattern = @"^(?:\+614|04)[0-9]{8}$|^(?:\+61|0)(2|3|7|8)[0-9]{8}$";
         
            return Regex.IsMatch(phoneNumber, pattern);
        }
        private async Task<bool> IsValidBusinessNumberAsync(string businessNumber)
        {
            if (string.IsNullOrEmpty(businessNumber))
            {
                return false;
            }

            await Task.Delay(1000); // simulate async business number validation

            return businessNumber.Length == 11;
        }
        private bool IsValidLoanAmount(decimal loanAmount)
        {
            return loanAmount >= _minLoanAmount && loanAmount < _maxLoanAmount;
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
        private string GetUniqueIdentifier(LoanApplicationAssessmentRequest loanApplication)
        {
            var builder = new StringBuilder();
            builder.Append(loanApplication.FirstName ?? "");
            builder.Append(loanApplication.LastName ?? "");
            builder.Append(loanApplication.EmailAddress ?? "");
            builder.Append(loanApplication.PhoneNumber ?? "");
            builder.Append(loanApplication.BusinessNumber ?? "");
            builder.Append(loanApplication.LoanAmount.ToString() ?? "");
            builder.Append(loanApplication.CitizenshipStatus ?? "");
            builder.Append(loanApplication.TimeTrading.ToString() ?? "");
            builder.Append(loanApplication.CountryCode ?? "");
            builder.Append(loanApplication.Industry ?? "");

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));

            return BitConverter.ToString(hash).Replace("-", "");
        }


    }
 }
