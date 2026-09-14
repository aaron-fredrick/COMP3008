using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnotherController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetResult()
        {
            RestClient client = new RestClient("http://localhost:5141");
            RestRequest request = new RestRequest("/api/Values", Method.Get);
            RestResponse response = client.Execute(request);
            String value = JsonConvert.DeserializeObject<String>(response.Content);
            return Ok(value);
        }
    }
}
