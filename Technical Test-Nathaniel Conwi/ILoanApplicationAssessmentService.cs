using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Technical_Test_Nathaniel_Conwi
{
    public interface ILoanApplicationAssessmentService
    {
        Task<LoanAssessmentResult> AssessLoanApplicationAsync(LoanApplicationAssessmentRequest loanApplication);
    }
}
