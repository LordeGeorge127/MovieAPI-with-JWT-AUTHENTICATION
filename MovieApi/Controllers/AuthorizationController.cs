using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Models;
using MovieApi.Models.Domain;
using MovieApi.Models.DTO;
using MovieApi.Repository.Abstract;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MovieApi.Controllers
{
    [Route("api/[controller]/{action}")]
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;

        public AuthorizationController(DataContext dataContext , UserManager<AppUser> userManager, RoleManager <IdentityRole> roleManager , ITokenService tokenService)
        {
            _dataContext = dataContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var user = await _userManager.FindByNameAsync(loginModel.UserName);
            if (user != null && await _userManager.CheckPasswordAsync( user, loginModel.Password) )
             {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name , user.UserName),
                    new Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                foreach ( var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role , userRole));
                }
                var token = _tokenService.GetToken(authClaims);
                var refreshToken = _tokenService.GetRefreshToken();
                var tokenInfo = _dataContext.TokenInfo.FirstOrDefault(a => a.UserName == user.UserName);  
                if (tokenInfo == null) 
                {
                    var info = new TokenInfo
                    {
                        UserName = user.UserName,
                        RefreshToken = refreshToken,
                        RefreshTokenExpiry = DateTime.Now.AddDays(7)
                    };
                }
                else
                {
                    tokenInfo.RefreshToken = refreshToken;
                    tokenInfo.RefreshTokenExpiry = DateTime.Now.AddDays(7); 
                }
                try
                {
                    _dataContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
                return Ok(new LoginResponse
                 {
                    Name = user.UserName,
                    UserName = user.UserName,
                    Token = token.TokenString,
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo,
                    StatusCode =1,
                    Message = "Logged in Successfully" 

                });
            }
            //login failed condition
            return Ok(
                new LoginResponse
                {
                    StatusCode = 0,
                    Message = "Invalid UserName or Password",
                    Token = " ",
                    Expiration = null
                });
        }
        [HttpPost]
        public async Task<IActionResult> Registration([FromBody] RegistrationModel registrationModel)
        {
            var status = new Status();
            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.StatusMessage = "pLEASE PASS ALL Required Fields";
                return Ok (status);
            }
            //check if user exists
            var userExists =await _userManager.FindByNameAsync(registrationModel.UserName);
            if (userExists != null)
            {
                status.StatusCode= 0;
                status.StatusMessage = "Invalid UserName";
                return Ok(status);
            }
            var user = new AppUser
            {
                UserName = registrationModel.UserName,
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = registrationModel.Email,
                Name = registrationModel.Name

            };
            //create a user here
            var result = await _userManager.CreateAsync(user, registrationModel.Password);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.StatusMessage = "Failed to Create User";
                return Ok(status);
            }
            //add roles here
            //for admin user UserRoles.Admin  
            if (!await _roleManager.RoleExistsAsync(UserRoles.User))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));
            if (await _roleManager.RoleExistsAsync(UserRoles.User))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.User);
            }
            status.StatusCode = 1;
            status.StatusMessage = "User Created Successfully";
            return Ok(status);

        }

        ////register admin
        //[HttpPost]
        //public async Task<IActionResult> RegisterAdmin([FromBody] RegistrationModel registrationModel)
        //{
        //    var status = new Status();
        //    if (!ModelState.IsValid)
        //    {
        //        status.StatusCode = 0;
        //        status.StatusMessage = "pLEASE PASS ALL Required Fields";
        //        return Ok(status);
        //    }
        //    //check if user exists
        //    var userExists = await _userManager.FindByNameAsync(registrationModel.UserName);
        //    if (userExists != null)
        //    {
        //        status.StatusCode = 0;
        //        status.StatusMessage = "Invalid UserName";
        //        return Ok(status);
        //    }
        //    var user = new AppUser
        //    {
        //        UserName = registrationModel.UserName,
        //        SecurityStamp = Guid.NewGuid().ToString(),
        //        Email = registrationModel.Email,
        //        Name = registrationModel.Name

        //    };
        //    //create a user here
        //    var result = await _userManager.CreateAsync(user, registrationModel.Password);
        //    if (!result.Succeeded)
        //    {
        //        status.StatusCode = 0;
        //        status.StatusMessage = "Failed to Create User";
        //        return Ok(status);
        //    }
        //    //add roles here
        //    //for admin user UserRoles.Admin  
        //    if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
        //        await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
        //    if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
        //    {
        //        await _userManager.AddToRoleAsync(user, UserRoles.Admin);
        //    }
        //    status.StatusCode = 1;
        //    status.StatusMessage = "User Created Successfully";
        //    return Ok(status);

        //}
        [HttpPost]
        public async Task<IActionResult>Changepassword(ChangePasswordModel changePasswordModel)
        {
            var status = new Status();
            //check validation

            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.StatusMessage = "Please pass all required fields";
                return Ok(status);
            }
            //find user
            var user = await _userManager.FindByNameAsync(changePasswordModel.UserName);
            if (user == null)
            {
                status.StatusCode = 0;
                status.StatusMessage = "Invalid username";
                return Ok(status);
            }
            //check current password
            if (!await _userManager.CheckPasswordAsync(user, changePasswordModel.CurrentPassword))
            {
                status.StatusCode = 0;
                status.StatusMessage = "Invalid Current Password";
                return Ok(status);
            }
            //change password here
            var result = await _userManager.ChangePasswordAsync(user, changePasswordModel.CurrentPassword, changePasswordModel.NewPassword);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.StatusMessage = "IFailed to change password";
                return Ok(status);
            }
            status.StatusCode = 1;
            status.StatusMessage = "Password changed successfully";
            return Ok(status);

        }
            
    }
}
