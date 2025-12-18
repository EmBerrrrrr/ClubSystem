using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Models;
using Service.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service hỗ trợ xác thực: Tạo JWT token, hash/verify mật khẩu.
    /// Không chứa logic đăng nhập/đăng ký (thuộc IAuthBusinessService).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Tạo JWT token cho user sau khi đăng nhập thành công.
        /// Token có thời hạn 3 giờ.
        /// </summary>
        public string GenerateToken(Account account, List<string> roles)
        {
            var jwtSection = _config.GetSection("Jwt");
            var keyString = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key chưa được cấu hình.");
            var key = Encoding.UTF8.GetBytes(keyString);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.Username ?? string.Empty),
                new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
                new Claim("fullName", account.FullName ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Hash mật khẩu bằng BCrypt.
        /// </summary>
        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        /// <summary>
        /// Kiểm tra mật khẩu có khớp với hash không.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}