using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helper
{
    public class PayOSSettings
    {
        public required string ClientId { get; set; }
        public required string ApiKey { get; set; }
        public required string ChecksumKey { get; set; }
        public required string BaseUrl { get; set; }
    }

}
