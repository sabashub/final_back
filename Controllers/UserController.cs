using final_backend.Models;
using final_backend.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using System.Security.Claims;

namespace final_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly Pkg_users _pkg_users;
        private readonly ITokenService _tokenService;

        public UserController(Pkg_users pkg_user, ITokenService tokenService)
        {
            _pkg_users = pkg_user;
            _tokenService=tokenService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            try
            {
                _pkg_users.RegisterUser(user);
                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request ) 
        {
            try
            {
                var user = _pkg_users.LoginUser(request.UserName, request.Password);
                UserResponse userResp = new UserResponse();

                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                // Generate JWT token using TokenService
                string token = _tokenService.CreateToken(user);
                userResp.Email = user.UserName;
                userResp.JWT = token;
                userResp.UserName = user.UserName;
                userResp.UserRole = user.UserRole;

                // Return token, email, and username as part of the response
                return Ok(userResp);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("get_users")]
        public ActionResult<List<User>> GetUsers()
        {
            var users = _pkg_users.GetUsers();
            return Ok(users);
        }

        [HttpGet("get_user"), Authorize]
        public ActionResult<User> GetUser()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim == null)
                {
                    return BadRequest("User not found in token.");
                }

                if (!int.TryParse(userIdClaim, out int parsedUserId))
                {
                    return BadRequest("Invalid user ID.");
                }

                var user = _pkg_users.GetUser(parsedUserId);

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                return Ok(_pkg_users.GetUser(parsedUserId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet("get_results/{id}")]
        public ActionResult<List<Result>> GetAnswers(int id)
        {
            var results = _pkg_users.GetResults(id);
            if (results == null || !results.Any())
            {
                return NotFound();
            }
            return Ok(results);
        }
        [HttpPost("add_result")]
        public IActionResult AddResult([FromBody] AddResult request)
        {
            if (request == null || string.IsNullOrEmpty(request.name) || request.questionsAndAnswers == null || !request.questionsAndAnswers.Any())
            {
                return BadRequest(new { message = "Invalid JSON data." });
            }

            // Convert to JSON string if needed or handle the data as per your logic
            var json = JsonConvert.SerializeObject(request);

            try
            {
                // Call the ADO.NET function to execute the stored procedure
                _pkg_users.AddResult(json);
                return Ok(new { message = "Result added successfully." });
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them)
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }


        [HttpPost("add_question")]
        public IActionResult AddQuestion([FromBody] Questions question)
        {
            try
            {
                // Call the ADO.NET function to add the question and get the updated list
                var questionsList = _pkg_users.AddQuestion(question);

                return Ok(new
                {
                    message = "Question added successfully",
                    questions = questionsList
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("get_names")]
        public ActionResult<List<Person>> GetNames()
        {
            var names = _pkg_users.GetNames();
            if (names == null || !names.Any())
            {
                return NotFound();
            }
            return Ok(names);
        }





        [HttpGet("get_questions")]
        public ActionResult<List<Questions>> GetQuestions()
        {
            var questions = _pkg_users.GetQuestions();
            return Ok(questions);
        }

        [HttpPut("edit_question{id}")]
        public IActionResult EditQuestion(int id, [FromBody] Questions question)
        {
            try
            {
                _pkg_users.EditQuestion(id, question);
                return Ok(new { message = "question updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




    }


    }
