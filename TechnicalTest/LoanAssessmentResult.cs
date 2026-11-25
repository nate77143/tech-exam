using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechnicalTest
{
    public class LoanAssessmentResult
    {
        public List<ValidationResult> validationResults { get; set; }
        public string Decision { get; set; }
    }
    public enum Decision
    {
        Unknown,
        Qualified,
        Unqualified
    }

}
