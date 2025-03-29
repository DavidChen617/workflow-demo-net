using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace workflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Nothing to see here");
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Nothing to see here");
        }
    }
}