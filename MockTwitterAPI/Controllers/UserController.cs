using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockTwitterAPI.Models;
using MockTwitterAPI.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MockTwitterAPI.Controllers
{
    [ApiController]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;

        public UserController(ILogger<UserController> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IOptionsSnapshot<JwtSettings> jwtSettings)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("[controller]/register")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegistrationModel registration)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Received request to create account with username: {registration.Username}");

                var NewUser = new IdentityUser { UserName = registration.Username };
                var AccountCreationResult = await _userManager.CreateAsync(NewUser, registration.Password);
                if (AccountCreationResult.Succeeded)
                {
                    _logger.LogInformation($"Account created successfully");
                    //await _signInManager.SignInAsync(NewUser,false);
                    return StatusCode(201, "Account created. Use the login endpoint to login.");     //Created
                }
                StringBuilder Errors = new StringBuilder();
                Errors.Append("ErrorList:\n");
                foreach(var error in AccountCreationResult.Errors)
                {
                    Errors.Append($"{error.Description}\n");
                    _logger.LogError($"Error occured during account creation for username: {registration.Username}: {error.Description}");
                }
                return StatusCode(422,Errors.ToString());     //Unprocessable entity
            }
            //bad request. This occurs if the model fails validation
            return BadRequest("Model failed to validate. This is due to incorrect parameters. Parameters are expected to be in JSON format and located in the request body");
        }
        
        [HttpPost("[controller]/login")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(LoginModel loginDetails)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Attemping to log in user with username: {loginDetails.Username}");
                IdentityUser UserToLogin = _userManager.Users.SingleOrDefault(u => u.UserName == loginDetails.Username);
                if(UserToLogin == null)
                {
                    return NotFound("No account found with the specified username.");
                }
                var SignInResult = await _userManager.CheckPasswordAsync(UserToLogin, loginDetails.Password);

                if (SignInResult)
                {

                    return Ok(GenerateJwt(UserToLogin));
                }
                return BadRequest("Incorrect password.");
            }
            return BadRequest("Model failed to validate. This is due to incorrect parameters. Parameters are expected to be in JSON format and located in the request body");
        }

        [HttpPost("[controller]/loginstatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public IActionResult LoginStatus()
        {
            if (User == null)
            {
                return UnprocessableEntity("No data for this user. Please register an account.");
            }
            if (User.Identity.IsAuthenticated)
            {
                return Ok($"Token valid. You are logged in as {User.Identity.Name}.");
            }
            return Ok("Token not recognized. You are not logged in.");
        }

        private string GenerateJwt(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_jwtSettings.ExpirationInDays));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Issuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
