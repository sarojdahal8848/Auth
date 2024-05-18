using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Auth.Api.Db;
using Auth.Api.Dtos;
using Auth.Api.Interfaces;
using Auth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController: ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthController(
        ITokenService tokenService,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailService emailService,
        ApplicationDbContext context,
        TokenValidationParameters tokenValidationParameters)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _context = context;
        _tokenValidationParameters = tokenValidationParameters;
    }
    [HttpPost]
    public IActionResult SendEmail([FromBody] EmailRequestDto emailRequestDto)
    {
        _emailService.SendEmail(emailRequestDto);

        return Ok();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = new AppUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                EmailConfirmed = false
            };

            var createdUser = await _userManager.CreateAsync(user, registerDto.Password!);
            if (!createdUser.Succeeded) return BadRequest(createdUser.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var email_body = "Please confirm your email address <a href=\"#URL#\">Click Here</a><br/> If above link does not work, please copy and paste the following link into your browser: <br/> #URL#";
            var callback_url = Request.Scheme + "://" + Request.Host + Url.Action(
                "ConfirmEmail",
                "Auth",
                new { userId = user.Id, code = code });
            var body = email_body.Replace("#URL#", System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callback_url));

            var confirmMail = new EmailRequestDto()
            {
                To = user.Email!,
                Subject = "Email Confirmation",
                Body = body

            };
            var sentEmail = _emailService.SendEmail(confirmMail);
            return !sentEmail ? StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong") : Ok("Confirmation email is sent. Please confirm your email.");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        try
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if(user == null)
                return Unauthorized("Email or password incorrect");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if(!result.Succeeded)
                return Unauthorized("Email or password incorrect");

            if (!user.EmailConfirmed)
                return Unauthorized("Email is not confirmed.");

            var token = await _tokenService.CreateToken(user);

            return Ok(token);

        }
        catch (Exception e)
        {

            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenDto)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid Parameters");
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshTokenDto.RefreshToken);
        if (storedToken == null) return BadRequest("Invalid Token");
        if(storedToken.ExpiryDate < DateTime.Now) {
            _context.RefreshTokens.Remove(storedToken);
            await _context.SaveChangesAsync();
            return BadRequest("Expired Token");
        }
        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if(user == null) return BadRequest("Invalid Token");
        var result = await _tokenService.CreateToken(user);

        return Ok(result);
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto resendConfirmationDto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(resendConfirmationDto.Email);
            if(user == null) return BadRequest("User not found");

            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (isEmailConfirmed) return BadRequest("Email is already confirmed");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var email_body = "Please confirm your email address <a href=\"#URL#\">Click Here</a><br/> If above link does not work, please copy and paste the following link into your browser: <br/> #URL#";
            var callback_url = Request.Scheme + "://" + Request.Host + Url.Action(
                "ConfirmEmail",
                "Auth",
                new { userId = user.Id, code = code });
            var body = email_body.Replace("#URL#", System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callback_url));

            var confirmMail = new EmailRequestDto()
            {
                To = user.Email!,
                Subject = "Email Confirmation",
                Body = body

            };
            var sentEmail = _emailService.SendEmail(confirmMail);
            return !sentEmail ? StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong") : Ok("Confirmation email is sent. Please confirm your email.");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }




    }


    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId is null || code is null)
            return BadRequest("Invalid email confirmation url");

        var user = await _userManager.FindByIdAsync(userId);
        if(user is null) return BadRequest("Invalid email parameter");

        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (isEmailConfirmed) return BadRequest("Email is already confirmed");

        var result = await _userManager.ConfirmEmailAsync(user, code);
        var status = result.Succeeded
            ? "Thank you for confirming your email."
            : "Your email is not confirmed. Please try again";
        return Ok(status);
    }

    // private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    // {
    //     var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    //     dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
    //     return dateTimeVal;
    // }
}