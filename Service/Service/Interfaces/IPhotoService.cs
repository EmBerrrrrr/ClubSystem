using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IPhotoService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file);
        Task DeleteImageAsync(string publicId);
    }
}
