using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechnicalTest
{
    public interface ILoanApplicationAssessmentService
    {
        Task<LoanAssessmentResult> AssessLoanApplicationAsync(LoanApplicationAssessmentRequest loanApplication);
    }
}
