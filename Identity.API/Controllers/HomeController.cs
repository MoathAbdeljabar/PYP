using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Welcome()
        {
            //foreach (var claim in User.Claims)
            //{
            //    Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}, Issuer: {claim.Issuer}");
            //}

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine(userId);
            return Ok("Welcome !!");

        }




        [HttpGet("hello")]
        public async Task<IActionResult> Test([FromQuery] Person p)
        {
            Console.WriteLine(p.Name);
            return Ok(p.Name);
        }


    }
}



public class Person
{
    public string? Name { get; set; }
}