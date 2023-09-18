using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using OperationCHAN.Controllers;
using OperationCHAN.Data;
using OperationCHAN.Models;

namespace OperationCHAN.Web.Api.Endpoints;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly TokenService _tokenService;
    private readonly RolesController _rolesController;
    
    
    public AuthController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, TokenService tokenService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _rolesController = new RolesController(db, _userManager);
    }
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(AuthModels.RegistrationRequest request)
    {
        LongPollingController.PrintRequest(HttpContext, request.ToJson());
        
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = _db.Users.FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower());
        if (email != null)
        {
            ModelState.AddModelError("BadEmail", "Email must be unique");
            return BadRequest(ModelState);
        }
        var user = _db.Users.FirstOrDefault(u => u.Nickname.ToLower() == request.Nickname.ToLower());
        if (user != null)
        {
            ModelState.AddModelError("BadNickname", "Nickname must be unique");
            return BadRequest(ModelState);
        }

        var result = await _userManager.CreateAsync(
            new ApplicationUser() { 
                UserName = request.Nickname.Replace(" ", ""),
                Nickname = request.Nickname,
                Email = request.Email.ToLower(),
                DiscordTag = request.DiscordTag
            },
            
            request.Password
        );
        if (result.Succeeded)
        {
            return Ok(new AuthModels.RegistrationResponse()
            {
                Nickname = request.Nickname,
                Email = request.Email,
                DiscordTag = request.DiscordTag
            });
        }
        foreach (var error in result.Errors) {
            ModelState.AddModelError(error.Code, error.Description);
        }
        return BadRequest(ModelState);
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<AuthModels.AuthResponse>> Authenticate([FromBody] AuthModels.AuthRequest request)
    {
        LongPollingController.PrintRequest(HttpContext, request.ToJson());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var managedUser = await _userManager.FindByEmailAsync(request.Email.ToLower());
        if (managedUser == null)
        {
            return Unauthorized();
        }
        var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password);
        if (!isPasswordValid)
        {
            return Unauthorized();
        }
        var userInDb = _db.Users.FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower());
        if (userInDb is null)
            return Unauthorized();
        
        var role = _rolesController.GetRole(userInDb.Id);
        var accessToken = _tokenService.CreateToken(userInDb);

        await _db.SaveChangesAsync();
        return Ok(new AuthModels.AuthResponse
        {
            Id = userInDb.Id,
            Nickname = userInDb.Nickname,
            Email = userInDb.Email.ToLower(),
            Token = accessToken,
            Role = role,
            DiscordTag = userInDb.DiscordTag
        });
    }
    
    [HttpPost]
    [Route("discord/register")]
    public async Task<IActionResult> RegisterDiscord(AuthModels.DiscordRegistrationRequest request)
    {
        LongPollingController.PrintRequest(HttpContext, request.ToJson());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = _db.Users.FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower());
        if (email != null)
        {
            ModelState.AddModelError("BadEmail", "Email must be unique");
            return BadRequest(ModelState);
        }
        var user = _db.Users.FirstOrDefault(u => u.Nickname == request.Nickname);
        if (user != null)
        {
            ModelState.AddModelError("BadNickname", "Nickname must be unique");
            return BadRequest(ModelState);
        }

        var result = await _userManager.CreateAsync(
            new ApplicationUser() { 
                UserName = request.Nickname.Replace(" ", ""),
                Nickname = request.Nickname,
                Email = request.Email.ToLower(),
                DiscordTag = request.DiscordTag,
                DiscordLogin = true,
                DiscordId = request.DiscordId
            }
        );
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(Register), new {email = request.Email.ToLower()}, request);
        }
        foreach (var error in result.Errors) {
            ModelState.AddModelError(error.Code, error.Description);
        }
        return BadRequest(ModelState);
    }
    
    [HttpPost]
    [Route("discord/login")]
    public async Task<ActionResult<AuthModels.AuthResponse>> AuthenticateDiscord([FromBody] AuthModels.DiscordAuthRequest request)
    {
        LongPollingController.PrintRequest(HttpContext, request.ToJson());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userInDb = _db.Users.FirstOrDefault(u => u.DiscordId == request.DiscordId);
        if (userInDb == null)
        {
            return Unauthorized();
        }

        var role = _rolesController.GetRole(userInDb.Id);
        var accessToken = _tokenService.CreateToken(userInDb);

        await _db.SaveChangesAsync();
        return Ok(new AuthModels.AuthResponse
        {
            Id = userInDb.Id,
            Nickname = userInDb.Nickname,
            Email = userInDb.Email.ToLower(),
            Token = accessToken,
            Role = role,
            DiscordTag = userInDb.DiscordTag
        });
    }
}