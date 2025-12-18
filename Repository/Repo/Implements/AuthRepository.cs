using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements;

public class AuthRepository : IAuthRepository
{
    /// <summary>
    /// Repository xử lý các thao tác liên quan đến xác thực và tài khoản người dùng (Account).
    /// 
    /// Công dụng: Truy vấn và cập nhật thông tin tài khoản, vai trò (roles), dùng cho đăng nhập/đăng ký/cập nhật profile.
    /// 
    /// Luồng dữ liệu:
    /// - Được gọi từ service (ví dụ: AuthBusinessService.LoginAsync) → Query DB bảng Account để lấy theo username/email.
    /// - AddAccountAsync → Lưu mới Account vào DB (Id auto-increment, Username/PasswordHash/Email...).
    /// - AddAccountRoleAsync → Lưu AccountRole (liên kết AccountId và RoleId) vào bảng AccountRoles.
    /// - UpdateAccountAsync → Update fields như FullName/Email/Phone/Status (không thay đổi password ở đây).
    /// - RemoveAccountRoleAsync → Xóa AccountRole nếu tồn tại.
    /// 
    /// Tương tác giữa các API/service:
    /// - Đăng ký (API /auth/register): AddAccountAsync + AddAccountRoleAsync (default role "student").
    /// - Đăng nhập (API /auth/login): GetAccountByUsernameAsync + GetRolesByAccountIdAsync → Verify password ở service.
    /// - Cập nhật profile (API /account/profile): GetAccountByIdAsync → UpdateAccountAsync.
    /// - Admin quản lý user: GetAllAccountsAsync → List all → UpdateAccountAsync (lock/unlock) hoặc RemoveAccountRoleAsync.
    /// </summary>
    private readonly StudentClubManagementContext _context;

    public AuthRepository(StudentClubManagementContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy Account theo Username (dùng cho login).
    /// 
    /// Luồng: Query bảng Account → Trả null nếu không tồn tại.
    /// </summary>
    public async Task<Account?> GetAccountByUsernameAsync(string username)
        => await _context.Accounts
            .SingleOrDefaultAsync(a => a.Username == username);

    /// <summary>
    /// Lấy Account theo Email (dùng cho register/check duplicate).
    /// 
    /// Luồng: Query bảng Account → Trả null nếu không tồn tại.
    /// </summary>
    public async Task<Account?> GetAccountByEmailAsync(string email)
        => await _context.Accounts
            .SingleOrDefaultAsync(a => a.Email == email);

    /// <summary>
    /// Lấy list roles của Account (ví dụ: "student", "clubleader").
    /// 
    /// Luồng: Join bảng AccountRoles → Roles → Trả list tên role.
    /// </summary>
    public async Task<List<string>> GetRolesByAccountIdAsync(int accountId)
    {
        return await _context.AccountRoles
            .Where(ar => ar.AccountId == accountId)
            .Select(ar => ar.Role.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Thêm Account mới vào DB (dùng cho register).
    /// 
    /// Luồng: Add vào bảng Account → SaveChanges (Id auto-gen).
    /// </summary>
    public async Task AddAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Lấy RoleId theo tên role (ví dụ: "student" → Id=1).
    /// 
    /// Luồng: Query bảng Roles → Throw nếu không tìm thấy.
    /// </summary>
    public async Task<int> GetRoleIdByNameAsync(string roleName)
    {
        var role = await _context.Roles
            .SingleAsync(r => r.Name == roleName);
        return role.Id;
    }

    /// <summary>
    /// Thêm AccountRole (liên kết account và role).
    /// 
    /// Luồng: Add vào bảng AccountRoles → SaveChanges.
    /// </summary>
    public async Task AddAccountRoleAsync(int accountId, int roleId)
    {
        var ar = new AccountRole
        {
            AccountId = accountId,
            RoleId = roleId
        };
        _context.AccountRoles.Add(ar);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Lấy tất cả Account (dùng cho admin list users).
    /// 
    /// Luồng: Query AsNoTracking để read-only → List toàn bộ.
    /// </summary>
    public async Task<List<Account>> GetAllAccountsAsync()     
    {
        return await _context.Accounts
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Lấy Account theo Id (dùng cho profile/update).
    /// 
    /// Luồng: Query bảng Account → Trả null nếu không tồn tại.
    /// </summary>
    public async Task<Account?> GetAccountByIdAsync(int id)    
    {
        return await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>
    /// Cập nhật Account (FullName/Email/Phone/Status...).
    /// 
    /// Luồng: Update entity → SaveChanges.
    /// </summary>

    public async Task UpdateAccountAsync(Account account)  
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa AccountRole (remove role khỏi account).
    /// 
    /// Luồng: Query AccountRoles → Nếu tồn tại thì Remove → SaveChanges.
    /// </summary>
    public async Task RemoveAccountRoleAsync(int accountId, int roleId) 
    {
        var ar = await _context.AccountRoles
            .SingleOrDefaultAsync(x => x.AccountId == accountId && x.RoleId == roleId);

        if (ar != null)
        {
            _context.AccountRoles.Remove(ar);
            await _context.SaveChangesAsync();
        }
    }
}
