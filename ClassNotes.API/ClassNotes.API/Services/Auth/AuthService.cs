using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Xml.Linq;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Auth;
using ClassNotes.API.Dtos.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ClassNotes.API.Services.TagsActivities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ClassNotes.API.Services.Auth
{
    // --------------------- CP --------------------- //
	public class AuthService : IAuthService
	{
        //Declaracion de las Variables Globales 
		private readonly SignInManager<UserEntity> _signInManager;
		private readonly UserManager<UserEntity> _userManager;
		private readonly IConfiguration _configuration;
		private readonly ILogger<AuthService> _logger;
        private readonly ClassNotesContext _context;
		private readonly ITagsActivitiesService _tagsActivitiesService;

		public AuthService(
			SignInManager<UserEntity> signInManager,
			UserManager<UserEntity> userManager,
			IConfiguration configuration,
			ILogger<AuthService> logger,
            ClassNotesContext context,
            ITagsActivitiesService tagsActivitiesService
			)
		{

			_signInManager = signInManager;
			_userManager = userManager;
			_configuration = configuration;
			_logger = logger;
            _context = context;
			_tagsActivitiesService = tagsActivitiesService;
		}

		public async Task<ResponseDto<LoginResponseDto>> LoginAsync(LoginDto dto)
		{
            // Método que verifica si el usuario puede iniciar sesión con las credenciales dadas.
            var result = await _signInManager.PasswordSignInAsync(
                dto.Email,
				dto.Password,
				isPersistent: false,
				lockoutOnFailure: false
            );

            /*  
            El resultado(result) puede ser:
                SignInResult.Success → Inicio de sesión exitoso.
                SignInResult.Failed → Credenciales incorrectas.
                SignInResult.LockedOut → La cuenta está bloqueada.
                SignInResult.RequiresTwoFactor → Se requiere autenticación de dos factores(2FA).
            */

			if (result.Succeeded)
			{
				// Generación del token
				var userEntity = await _userManager.FindByEmailAsync(dto.Email);

				// Creación de la lista de las claims
				List<Claim> authClaims = await GetClaims(userEntity);

				var jwtToken = GetToken(authClaims);

				var refreshToken = GenerateRefreshTokenString();

                userEntity.RefreshToken = refreshToken;
                
                // Refresh dejado en 30 mins
                
                userEntity.RefreshTokenExpire = DateTime.Now
                    .AddMinutes(int.Parse(_configuration["JWT:RefreshTokenExpire"] ?? "30"));

                _context.Entry(userEntity);

                await _context.SaveChangesAsync();

				return new ResponseDto<LoginResponseDto>
				{
					StatusCode = 200,
					Status = true,
					Message = MessagesConstant.LOGIN_SUCCESS,
					Data = new LoginResponseDto
					{
						FullName = $"{userEntity.FirstName} {userEntity.LastName}",
						Email = userEntity.Email,
						Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
						TokenExpiration = jwtToken.ValidTo,
						RefreshToken = refreshToken
					}
				};
			}

			// TODO: Validar las tipos de respuestas como Credenciales Incorrectas o cuenta Bloqueada.

			return new ResponseDto<LoginResponseDto>
			{
				Status = false,
				StatusCode = 401,
				Message = MessagesConstant.LOGIN_ERROR
			};
		}

		private async Task<List<Claim>> GetClaims(UserEntity userEntity)
        {
            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, userEntity.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("UserId", userEntity.Id),
                    new Claim("DefaultConfigCourse", (userEntity.DefaultCourseSettingId ?? null).ToString())
                };

            var userRoles = await _userManager.GetRolesAsync(userEntity);

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            return authClaims;
        }

        public async Task<ResponseDto<LoginResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            string email = "";
            try 
            {
                var principal = GetTokenPrincipal(dto.Token);

                /* Busca dentro de principal.Claims el primer claim donde el tipo sea "emailaddress".
                   Si existe, se almacena en emailClaim, si no, será null.*/

                var emailClaim = principal.Claims.FirstOrDefault(c => 
                    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"); // ASP.NET Identity usa estos esquemas para representar ciertos claims.
                
                var userIdCLaim = principal.Claims.Where(x => x.Type == "UserId").FirstOrDefault();
                
                if (emailClaim is null) 
                {
                    return new ResponseDto<LoginResponseDto> 
                    {
                        StatusCode = 401,
                        Status = false,
                        Message = MessagesConstant.INCORRECT_EMAIL
                    };
                }

                email = emailClaim.Value;

                var userEntity = await _userManager.FindByEmailAsync(email);

                if (userEntity is null) 
                {
                    return new ResponseDto<LoginResponseDto> 
                    {
                        StatusCode = 401,
                        Status = false,
                        Message = MessagesConstant.USER_RECORD_NOT_FOUND
                    };
                }

                if (userEntity.RefreshToken  != dto.RefreshToken) 
                {
                    return new ResponseDto<LoginResponseDto>
                    {
                        StatusCode = 401,
                        Status = false,
                        Message = MessagesConstant.LOGIN_ERROR
                    };
                }

                if (userEntity.RefreshTokenExpire < DateTime.Now) 
                {
                    return new ResponseDto<LoginResponseDto>
                    {
                        StatusCode = 401,
                        Status = false,
                        Message = MessagesConstant.TOKEN_EXPIRED
                    };
                }

                List<Claim> authClaims = await GetClaims(userEntity);

                var jwtToken = GetToken(authClaims);

                var loginResponseDto = new LoginResponseDto 
                {
                    Email = email,
                    FullName = $"{userEntity.FirstName} {userEntity.LastName}",
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    TokenExpiration = jwtToken.ValidTo,
                    RefreshToken = GenerateRefreshTokenString()
                };

                userEntity.RefreshToken = loginResponseDto.RefreshToken;

                userEntity.RefreshTokenExpire = DateTime.Now
                    .AddMinutes(int.Parse(_configuration["JWT:RefreshTokenExpire"] ?? "30"));

                _context.Entry(userEntity);

                await _context.SaveChangesAsync();

                return new ResponseDto<LoginResponseDto> 
                {
                    StatusCode = 200,
                    Status = true,
                    Message = MessagesConstant.USER_REGISTERED_SUCCESS,
                    Data = loginResponseDto
                };
            } 
            catch (Exception e) 
            {
                _logger.LogError(exception: e, message: e.Message);

                return new ResponseDto<LoginResponseDto> 
                {
                    StatusCode = 500,
                    Status = false,
                    Message = MessagesConstant.USER_REGISTRATION_FAILED
                };
            }
        }
        
        // Funcion para obtener un refresh Token
        private string GenerateRefreshTokenString()
        {
            var randomNumber  = new byte[64];

            using (var numberGenerator = RandomNumberGenerator.Create())
            {
                numberGenerator.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }
		
        public async Task<ResponseDto<LoginResponseDto>> RegisterAsync(RegisterDto dto)
		{
			var user = new UserEntity 
            {
				FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Email,
                Email = dto.Email,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);


            if (result.Succeeded)
            {
                var userEntity = await _userManager.FindByEmailAsync(dto.Email);

                await _userManager.AddToRoleAsync(userEntity, RolesConstant.USER);

				var authClaims = await GetClaims(userEntity);

                var jwtToken = GetToken(authClaims);

				var refreshToken = GenerateRefreshTokenString();
                userEntity.RefreshToken = refreshToken;
                userEntity.RefreshTokenExpire = DateTime.Now
                    .AddMinutes(int.Parse(_configuration["JWT:RefreshTokenExpire"] ?? "30"));
                _context.Entry(userEntity);

				await _context.SaveChangesAsync();

				// AM: Crear las tags predeterminadas para el nuevo usuario
				await _tagsActivitiesService.CreateDefaultTagsAsync(userEntity.Id);

				return new ResponseDto<LoginResponseDto> 
                {
                    StatusCode = 200,
                    Status = true,
                    Message = MessagesConstant.USER_REGISTERED_SUCCESS,
                    Data = new LoginResponseDto 
                    {
						FullName = $"{userEntity.FirstName} {userEntity.LastName}",
                        Email = userEntity.Email,
                        Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        TokenExpiration = jwtToken.ValidTo,
						RefreshToken = refreshToken,
                    }
                };
            }

            return new ResponseDto<LoginResponseDto> 
            {
                StatusCode = 400,
                Status = false,
                Message = MessagesConstant.USER_REGISTRATION_FAILED
            };
		}

		private JwtSecurityToken GetToken(List<Claim> authClaims)
		{
			var authSigninKey = new SymmetricSecurityKey(Encoding.UTF8
				.GetBytes(_configuration["JWT:Secret"]));

			return new JwtSecurityToken(
				issuer: _configuration["JWT:ValidIssuer"],
				audience: _configuration["JWT:ValidAudience"],
				expires: DateTime.Now.AddMinutes(int.Parse(_configuration["JWT:Expires"] ?? "15")),
				claims: authClaims,
				signingCredentials: new SigningCredentials(authSigninKey,
					SecurityAlgorithms.HmacSha256)
			);
		}

		public ClaimsPrincipal GetTokenPrincipal(string token)
        {
            var securityKey = new SymmetricSecurityKey(Encoding
                .UTF8.GetBytes(_configuration.GetSection("JWT:Secret").Value));

            var validation = new TokenValidationParameters 
            {
                IssuerSigningKey = securityKey,
                ValidateLifetime = false,
                ValidateActor = false,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
        }

		// --------------------- CP --------------------- //
	}
}
