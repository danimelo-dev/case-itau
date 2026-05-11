using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Funds.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken()
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var secretKey = Encoding.UTF8.GetBytes(
            jwtSettings["SecretKey"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Daniel Melo"),
                new Claim(ClaimTypes.Role, "Admin")
            }),

            Expires = DateTime.UtcNow.AddHours(1),

            Issuer = jwtSettings["Issuer"],

            Audience = jwtSettings["Audience"],

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secretKey),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new
        {
            access_token = tokenHandler.WriteToken(token)
        });
    }
}