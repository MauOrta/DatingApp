using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public IAuthRepository _repo { get; set; }
        public IConfiguration _config { get; set; }
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            this._config = config;
            this._repo = repo;

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegDto)
        {
            // validate request

            userForRegDto.userName = userForRegDto.userName.ToLower();

            if (await _repo.UserExists(userForRegDto.userName))
                return BadRequest("Username already exists");

            var userToCreate = new User()
            {
                UserName = userForRegDto.userName
            };

            var createdUser = await _repo.Register(userToCreate, userForRegDto.password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLogindto)
        {
           
                throw new Exception("Computer says no!!");
                var userFromRepo = await _repo.Login(userForLogindto.Username.ToLower(), userForLogindto.password);

                if (userFromRepo == null)
                    return Unauthorized();

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.UserName)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8
                        .GetBytes(_config.GetSection("AppSettings:Token").Value));

                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var tokenDescriptor = new SecurityTokenDescriptor() {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddDays(1),
                    SigningCredentials = creds

                };

                var tokenHandler = new JwtSecurityTokenHandler();

                var token = tokenHandler.CreateToken(tokenDescriptor);

                return Ok(new {
                    token = tokenHandler.WriteToken(token)
                });
  
        }
    }
}