using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Technical_Test_Nathaniel_Conwi
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
