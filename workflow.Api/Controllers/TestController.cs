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

        [HttpGet("test2")]
        public IActionResult Test2()
        {
            return Ok("Nothing to see here");
        }

        [HttpGet("test3")]
        public IActionResult Test3()
        {
            return Ok("Nothing to see here");
        }

        [HttpGet("test4")]
        public IActionResult Test4()
        {
            return Ok("Nothing to see here");
        }

        [HttpGet("test5")]
        public IActionResult Test5()
        {
            return Ok("Nothing to see here");
        }

    }
}