using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Technical_Test_Nathaniel_Conwi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoanApplicationAssessmentController : ControllerBase
    {
        private readonly ILoanApplicationAssessmentService _loanApplicationAssessmentService;

        public LoanApplicationAssessmentController(ILoanApplicationAssessmentService loanApplicationAssessmentService)
        {
            _loanApplicationAssessmentService = loanApplicationAssessmentService;
        }

        [HttpPost]
        public async Task<IActionResult> AssessLoanApplication([FromBody] LoanApplicationAssessmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _loanApplicationAssessmentService.AssessLoanApplicationAsync(request);

            return Ok(result);
        }
    }
}