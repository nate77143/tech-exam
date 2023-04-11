using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Technical_Test_Nathaniel_Conwi
{
    public class LoanApplicationAssessmentRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string BusinessNumber { get; set; }
        public decimal LoanAmount { get; set; }
        public string CitizenshipStatus { get; set; }
        public int TimeTrading { get; set; }
        public string CountryCode { get; set; }
        public string Industry { get; set; }
    }
}
