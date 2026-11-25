using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechnicalTest
{
    public class ValidationResult
    {
        public ValidationResult(string _rule, string _message, string _decision)
        {
            Rule = _rule;
            Message = _message;
            Decision = _decision;

        }
        
        public string Rule { get; set; }
        public string Message { get; set; }
        public string Decision { get; set; }
    }
}
