﻿using Microsoft.AspNetCore.Http;
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
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;

        public UserController(ILogger<UserController> logger,
            UserManager<IdentityUser> userManager,
            IOptionsSnapshot<JwtSettings> jwtSettings)
        {
            _logger = logger;
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("[controller]/register")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] AccountModel registration)
        {
            //make sure model is valid
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Received request to create account with username: {registration.Username}");

                //create a new user object with username from supplied accountmodel
                var NewUser = new IdentityUser { UserName = registration.Username };
                //create the account with the new user and the password
                var AccountCreationResult = await _userManager.CreateAsync(NewUser, registration.Password);
                if (AccountCreationResult.Succeeded)
                {
                    _logger.LogInformation($"Account created successfully");
                    return CreatedAtAction("Register", new { Message = "Account created. Use the login endpoint to login." });     //Created
                }
                //if the account creation fails, return the errors to show why it fails
                StringBuilder Errors = new StringBuilder();
                Errors.Append("ErrorList:\n");
                foreach(var error in AccountCreationResult.Errors)
                {
                    Errors.Append($"{error.Description}\n");
                    _logger.LogError($"Error occured during account creation for username: {registration.Username}: {error.Description}");
                }
                return Problem(detail:Errors.ToString(),statusCode:422);     //Unprocessable entity
            }
            //bad request. This occurs if the model fails validation
            return Problem(detail:"Model failed to validate. This is due to incorrect parameters. Parameters are expected to be in JSON format and located in the request body", statusCode:400);
        }
        
        [HttpPost("[controller]/login")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Login([FromBody] AccountModel loginDetails)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Attemping to log in user with username: {loginDetails.Username}");
                //get the user's information
                IdentityUser UserToLogin = _userManager.Users.SingleOrDefault(u => u.UserName == loginDetails.Username);
                if(UserToLogin == null)
                {
                    return NotFound("No account found with the specified username.");
                }
                //verify their password
                var SignInResult = await _userManager.CheckPasswordAsync(UserToLogin, loginDetails.Password);

                if (SignInResult)
                {
                    // if the password is good generate a JWT for the user to authorize with
                    return Ok(new { token=GenerateJwt(UserToLogin) });
                }
                return Problem(detail:"Incorrect password.",statusCode:400);
            }
            return Problem(detail:"Model failed to validate. This is due to incorrect parameters. Parameters are expected to be in JSON format and located in the request body",statusCode:400);
        }

        [HttpPost("[controller]/loginstatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public IActionResult LoginStatus()
        {
            if (User == null)
            {
                return Problem(detail:"No data for this user. Please register an account.",statusCode:422);
            }
            //let the user know who they are logged in as (who the JWT belongs to)
            if (User.Identity.IsAuthenticated)
            {
                return Ok(new { message = $"Token valid. You are logged in as {User.Identity.Name}." });
            }
            return Problem(detail:"Token not recognized. You are not logged in.",statusCode:400);
        }

        private string GenerateJwt(IdentityUser user)
        {
            //this method taken from https://medium.com/swlh/securing-your-net-core-3-api-using-identity-93d6426d6311
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
