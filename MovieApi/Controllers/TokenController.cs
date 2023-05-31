using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Models.Domain;
using MovieApi.Repository.Abstract;

namespace MovieApi.Controllers
{
    [Route("api/[Controller]/{action}")]
    [ApiController]
    public class TokenController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ITokenService _tokenService;

        public TokenController(DataContext dataContext, ITokenService tokenService)
        {
            _dataContext = dataContext;
            _tokenService = tokenService;
        }
        [HttpPost]
        public IActionResult Refresh(RefreshTokenRequest tokenApiModel)
        {
            if (tokenApiModel == null)
            {
                return BadRequest("Invalid Client Request");
            }
            //string AccessToken = tokenApiModel.AccessToken;
            string refreshToken =tokenApiModel.RefreshToken;
            var principal = _tokenService.GetPrincipalFromExpiredToken(refreshToken);
            var username = principal.Identity.Name;
            var user = _dataContext.TokenInfo.SingleOrDefault(u=> u.UserName == username);
            if (user == null || user.RefreshTokenExpiry<= DateTime.Now)
            {
                return BadRequest("Invalid Client Request");
            }
            var newAcessToken = _tokenService.GetToken(principal.Claims);
            var newRefreshToken = _tokenService.GetRefreshToken();
            user.RefreshToken = newRefreshToken;
            _dataContext.SaveChanges();
            return Ok(new RefreshTokenRequest()
            {
                //AccessToken = newAcessToken.TokenString,
                RefreshToken = newRefreshToken,
            });
        }

        //remove token entry with revoke
        [HttpPost,Authorize]
        public IActionResult Revoke()
        {
            try
            {
                var username = User.Identity.Name;
                var user = _dataContext.TokenInfo.SingleOrDefault(u => u.UserName == username);
                if (user == null) return BadRequest();
                user.RefreshToken = null;
                _dataContext.SaveChanges();
                return Ok();
            }
            catch(Exception ex) 
            {
                return BadRequest();
            }
        }
    }
}
