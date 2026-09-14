using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SimpleWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculatorController : ControllerBase
    {
        [HttpGet]
        [Route("add/{firstNumber}/{secondNumber}")]
        public IActionResult Add(int firstNumber, int secondNumber)
        {
            int result = firstNumber + secondNumber;
            var response = new { Result = result};
            return Ok(response);
        }

        [Route("add")]
        [HttpGet]
        public IActionResult AddFromQuery([FromQuery]int firstNumber, 
            [FromQuery] int secondNumber)
        {
            int result = firstNumber + secondNumber;
            var response = new { Result = result };
            return Ok(response);
        }

        [HttpGet]
        [Route("subtract/{firstNumber}/{secondNumber}")]
        public IActionResult Subtract(int firstNumber, int secondNumber)
        {
            int result = firstNumber + secondNumber;
            var response = new { Result = result };
            return Ok(response);
        }

    }
}
