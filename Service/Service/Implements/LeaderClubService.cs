using DTO;
using DTO.DTO.Activity;
using DTO.DTO.Club;
using DTO.DTO.Membership;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Service.Interfaces;

public class LeaderClubService : ILeaderClubService
{
    private readonly StudentClubManagementContext _context;

    public LeaderClubService(StudentClubManagementContext context)
    {
        _context = context;
    }

    // Lấy tất cả club mà account đang là leader active
    public async Task<List<LeaderClubDTO>> GetClubsOfLeaderAsync(int leaderAccountId)
    {
        return await _context.Clubs
            .AsNoTracking()
            .Where(c => _context.ClubLeaders
                .Any(cl => cl.ClubId == c.Id
                        && cl.AccountId == leaderAccountId
                        && cl.IsActive == true))
            .Select(c => new LeaderClubDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                MembershipFee = c.MembershipFee,
                Status = c.Status
            })
            .ToListAsync();
    }

    public async Task<LeaderClubDTO> CreateClubAsync(int leaderAccountId, LeaderClubCreateDTO dto)
    {
        var club = new Club
        {
            Name = dto.Name,
            Description = dto.Description,
            EstablishedDate = dto.EstablishedDate.HasValue ? DateOnly.FromDateTime(dto.EstablishedDate.Value) : null,
            ImageClubsUrl = dto.ImageClubsUrl,
            MembershipFee = dto.MembershipFee,
            Status = "active"
        };

        var leader = new ClubLeader
        {
            AccountId = leaderAccountId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };

        // add both and save once to reduce DB roundtrips and ensure atomicity
        await _context.Clubs.AddAsync(club);
        // set leader.Club will be linked after SaveChanges, but set Club reference to club entity
        leader.Club = club;
        await _context.ClubLeaders.AddAsync(leader);

        await _context.SaveChangesAsync();

        return new LeaderClubDTO
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            MembershipFee = club.MembershipFee,
            Status = club.Status
        };
    }

    public async Task<bool> UpdateClubAsync(int leaderAccountId, int clubId, LeaderClubUpdateDTO dto)
    {
        var club = await _context.Clubs
            .Where(c => c.Id == clubId)
            .Where(c => _context.ClubLeaders
                .Any(cl => cl.ClubId == c.Id
                        && cl.AccountId == leaderAccountId
                        && cl.IsActive == true))
            .FirstOrDefaultAsync();

        if (club == null) return false;

        club.Name = dto.Name;
        club.Description = dto.Description;
        club.EstablishedDate = dto.EstablishedDate.HasValue ? DateOnly.FromDateTime(dto.EstablishedDate.Value) : null;
        club.ImageClubsUrl = dto.ImageClubsUrl;
        club.MembershipFee = dto.MembershipFee;
        club.Status = dto.Status ?? club.Status;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClubAsync(int leaderAccountId, int clubId)
    {
        var club = await _context.Clubs
            .Where(c => c.Id == clubId)
            .Where(c => _context.ClubLeaders
                .Any(cl => cl.ClubId == c.Id
                        && cl.AccountId == leaderAccountId
                        && cl.IsActive == true))
            .FirstOrDefaultAsync();

        if (club == null) return false;

        _context.Clubs.Remove(club);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<LeaderClubDTO?> GetClubOfLeaderAsync(int leaderAccountId, int clubId)
    {
        var club = await _context.Clubs
            .AsNoTracking()
            .Where(c => c.Id == clubId)
            .Where(c => _context.ClubLeaders
                .Any(cl => cl.ClubId == c.Id
                        && cl.AccountId == leaderAccountId
                        && cl.IsActive == true))
            .Select(c => new LeaderClubDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                MembershipFee = c.MembershipFee,
                Status = c.Status
            })
            .FirstOrDefaultAsync();

        return club;
    }

    // lấy list request của club (chỉ khi leader đang quản lý club đó)
    public async Task<List<MembershipRequestDTO>> GetMembershipRequestsAsync(
        int leaderAccountId, int clubId, string? status)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return new();

        var query = _context.MembershipRequests
            .AsNoTracking()
            .Where(r => r.ClubId == clubId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new MembershipRequestDTO
            {
                Id = r.Id,
                AccountId = r.AccountId,
                AccountName = r.Account.FullName,
                RequestDate = r.RequestDate,
                Status = r.Status!
            })
            .ToListAsync();
    }

    // approve: đổi status + tạo membership
    public async Task<bool> ApproveMembershipRequestAsync(
        int leaderAccountId, int clubId, int requestId)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var req = await _context.MembershipRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ClubId == clubId);
        if (req == null || req.Status != "pending") return false;

        req.Status = "approved";
        req.ProcessedBy = leaderAccountId;
        req.ProcessedAt = DateTime.UtcNow;

        bool hasMembership = await _context.Memberships
            .AnyAsync(m => m.AccountId == req.AccountId && m.ClubId == clubId);

        if (!hasMembership)
        {
            var mem = new Membership
            {
                AccountId = req.AccountId,
                ClubId = clubId,
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "active"
            };
            await _context.Memberships.AddAsync(mem);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // reject: đổi status, lưu lý do
    public async Task<bool> RejectMembershipRequestAsync(
        int leaderAccountId, int clubId, int requestId, string reason)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var req = await _context.MembershipRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ClubId == clubId);
        if (req == null || req.Status != "pending") return false;

        req.Status = "rejected";
        req.ProcessedBy = leaderAccountId;
        req.ProcessedAt = DateTime.UtcNow;
        req.Note = reason;

        await _context.SaveChangesAsync();
        return true;
    }

    // list member trong club
    public async Task<List<MembershipDTO>> GetMembersAsync(
        int leaderAccountId, int clubId)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return new();

        return await _context.Memberships
            .AsNoTracking()
            .Where(m => m.ClubId == clubId)
            .Select(m => new MembershipDTO
            {
                Id = m.Id,
                AccountId = m.AccountId,
                AccountName = m.Account.FullName,
                JoinDate = m.JoinDate.HasValue ? m.JoinDate.Value.ToDateTime(System.TimeOnly.MinValue) : (DateTime?)null,
                Status = m.Status!
            })
            .ToListAsync();
    }

    // đổi status member (active/inactive)
    public async Task<bool> UpdateMemberStatusAsync(
        int leaderAccountId, int clubId, int membershipId, string status)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var mem = await _context.Memberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.ClubId == clubId);
        if (mem == null) return false;

        mem.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    // kick member khỏi club
    public async Task<bool> RemoveMemberAsync(
        int leaderAccountId, int clubId, int membershipId)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var mem = await _context.Memberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.ClubId == clubId);
        if (mem == null) return false;

        _context.Memberships.Remove(mem);
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== ACTIVITIES =====

    public async Task<List<ActivityDTO>> GetActivitiesAsync(int leaderAccountId, int clubId)
    {
        // kiểm tra leader có quản lý club này không
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return new();

        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.StartTime)
            .Select(a => new ActivityDTO
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location,
                Status = a.Status,
                CreatedBy = a.CreatedBy,
                ApprovedBy = a.ApprovedBy
            })
            .ToListAsync();
    }

    public async Task<ActivityDTO?> CreateActivityAsync(
        int leaderAccountId, int clubId, ActivityCreateDTO dto)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return null;

        var activity = new Activity
        {
            ClubId = clubId,
            Title = dto.Title,
            Description = dto.Description,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Location = dto.Location,
            Status = "draft",
            CreatedBy = leaderAccountId
        };

        await _context.Activities.AddAsync(activity);
        await _context.SaveChangesAsync();

        return new ActivityDTO
        {
            Id = activity.Id,
            ClubId = activity.ClubId,
            Title = activity.Title,
            Description = activity.Description,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Location = activity.Location,
            Status = activity.Status,
            CreatedBy = activity.CreatedBy,
            ApprovedBy = activity.ApprovedBy
        };
    }

    public async Task<bool> UpdateActivityAsync(
        int leaderAccountId, int clubId, int activityId, ActivityUpdateDTO dto)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == activityId && a.ClubId == clubId);
        if (activity == null) return false;

        activity.Title = dto.Title;
        activity.Description = dto.Description;
        activity.StartTime = dto.StartTime;
        activity.EndTime = dto.EndTime;
        activity.Location = dto.Location;
        activity.Status = dto.Status ?? activity.Status;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteActivityAsync(
        int leaderAccountId, int clubId, int activityId)
    {
        bool isLeader = await _context.ClubLeaders
            .AnyAsync(cl => cl.ClubId == clubId
                         && cl.AccountId == leaderAccountId
                         && cl.IsActive == true);
        if (!isLeader) return false;

        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == activityId && a.ClubId == clubId);
        if (activity == null) return false;

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();
        return true;
    }
}
