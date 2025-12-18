using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Membership
{
    /// <summary>
    /// DTO để tạo membership request
    /// </summary>
    public class CreateMembershipRequestDto
    {
        /// <summary>
        /// ID của câu lạc bộ muốn tham gia
        /// </summary>
        public int ClubId { get; set; }
        
        /// <summary>
        /// Lý do tham gia câu lạc bộ (bắt buộc)
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Họ và tên (optional - chỉ cần nếu account chưa có thông tin này)
        /// </summary>
        public string? FullName { get; set; }
        
        /// <summary>
        /// Email (optional - chỉ cần nếu account chưa có thông tin này)
        /// </summary>
        public string? Email { get; set; }
        
        /// <summary>
        /// Số điện thoại (optional - chỉ cần nếu account chưa có thông tin này)
        /// </summary>
        public string? Phone { get; set; }
        
        /// <summary>
        /// Chuyên ngành (optional)
        /// </summary>
        public string? Major { get; set; }
        
        /// <summary>
        /// Kỹ năng (optional)
        /// </summary>
        public string? Skills { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
