using DTO;
using DTO.DTO.Auth;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;

namespace Service.Services;

public interface IAuthBusinessService
{
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);
    Task<LoginResponseDTO?> RegisterAsync(RegisterRequestDTO request);
    Task<LoginResponseDTO?> UpdateAccountAsync(int accountId, UpdateAccountRequestDto request);
}

public class AuthBusinessService : IAuthBusinessService
{
    private readonly IAuthRepository _repo;
    private readonly IAuthService _authService;

    public AuthBusinessService(IAuthRepository repo, IAuthService authService)
    {
        _repo = repo;
        _authService = authService;
    }

    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
    {
        var account = await _repo.GetAccountByUsernameAsync(request.Username);
        if (account == null)
            return null;

        if (account.IsActive == false)
            throw new Exception("ACCOUNT_LOCKED");

        if (!_authService.VerifyPassword(request.Password, account.PasswordHash))
            return null;

        var roles = await _repo.GetRolesByAccountIdAsync(account.Id);
        var token = _authService.GenerateToken(account, roles);

        return new LoginResponseDTO
        {
            Token = token,
            AccountId = account.Id,
            Username = account.Username,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName ?? string.Empty,
            Phone = account.Phone,
            Roles = roles
        };
    }


    public async Task<LoginResponseDTO?> RegisterAsync(RegisterRequestDTO request)
    {
        // 1. Check username trùng
        var existingUsername = await _repo.GetAccountByUsernameAsync(request.Username);
        if (existingUsername != null) 
            throw new Exception("Username already exists.");

        // 2. Check email trùng
        var existingEmail = await _repo.GetAccountByEmailAsync(request.Email);
        if (existingEmail != null) 
            throw new Exception("Email already exists.");

        // 3. Tạo account mới
        var account = new Account
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _repo.AddAccountAsync(account);

        // 4. Gán role mặc định "Student" trong bảng roles + account_roles
        const string defaultRoleName = "Student";

        // lấy roleId theo tên
        var roleId = await _repo.GetRoleIdByNameAsync(defaultRoleName);

        // thêm vào bảng account_roles
        await _repo.AddAccountRoleAsync(account.Id, roleId);

        var roles = new List<string> { defaultRoleName };

        // 5. Tạo token
        var token = _authService.GenerateToken(account, roles);

        // 6. Trả response
        return new LoginResponseDTO
        {
            Token = token,
            AccountId = account.Id,
            Username = account.Username,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName ?? string.Empty,
            Phone = account.Phone,
            Roles = roles
        };
    }

    public async Task<LoginResponseDTO?> UpdateAccountAsync(int accountId, UpdateAccountRequestDto request)
    {
        // 1. Lấy account hiện tại
        var account = await _repo.GetAccountByIdAsync(accountId);
        if (account == null)
            throw new Exception("Account not found.");

        // 2. Kiểm tra email trùng (nếu có thay đổi email)
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != account.Email)
        {
            var existingEmail = await _repo.GetAccountByEmailAsync(request.Email);
            if (existingEmail != null && existingEmail.Id != accountId)
                throw new Exception("Email already exists.");
        }

        // 3. Cập nhật thông tin (chỉ cập nhật các trường được cung cấp)
        if (!string.IsNullOrWhiteSpace(request.FullName))
            account.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.Email))
            account.Email = request.Email;

        // Phone: nếu gửi null hoặc empty thì giữ nguyên, chỉ update khi có giá trị hợp lệ
        if (request.Phone != null && !string.IsNullOrWhiteSpace(request.Phone))
            account.Phone = request.Phone;

        // 4. Cập nhật password nếu có (hash trước khi lưu)
        bool passwordChanged = false;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            account.PasswordHash = _authService.HashPassword(request.Password);
            passwordChanged = true;
        }

        // 5. Lưu thay đổi
        await _repo.UpdateAccountAsync(account);

        // 6. Lấy roles và tạo token mới
        var roles = await _repo.GetRolesByAccountIdAsync(account.Id);
        var token = _authService.GenerateToken(account, roles);

        // 7. Trả response
        return new LoginResponseDTO
        {
            Token = token,
            AccountId = account.Id,
            Username = account.Username,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName ?? string.Empty,
            Phone = account.Phone,
            Roles = roles,
            PasswordChanged = passwordChanged ? true : null // Chỉ set true nếu có đổi password, null nếu không
        };
    }
}
