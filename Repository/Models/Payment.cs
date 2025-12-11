using Repository.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int MembershipId { get; set; }

    public int ClubId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidDate { get; set; }

    public string Method { get; set; }

    public string Status { get; set; }

    public long? OrderCode { get; set; }  

    public string Description { get; set; }

    public virtual Club Club { get; set; }

    public virtual Membership Membership { get; set; }
}
