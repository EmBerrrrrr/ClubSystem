using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Service.Service.Interfaces;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service xử lý upload và xóa ảnh lên Cloudinary.
    /// Được dùng cho: Avatar user, ảnh activity, ảnh club.
    /// 
    /// Bảo mật: Validation nghiêm ngặt về file type, size, MIME.
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        /// <summary>
        /// Upload ảnh lên Cloudinary và trả về URL + PublicId.
        /// Validation: size ≤ 5MB, extension và MIME type hợp lệ.
        /// </summary>
        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống.");

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
                throw new ArgumentException($"Kích thước file vượt quá 5MB (hiện tại: {(file.Length / 1024.0 / 1024):F2}MB).");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".gif" };
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                throw new ArgumentException($"Định dạng file không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}");

            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic", "image/gif" };
            if (string.IsNullOrEmpty(file.ContentType) || !allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new ArgumentException("File phải là định dạng ảnh hợp lệ.");

            string? uploadedPublicId = null;

            try
            {
                await using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "studentclub",
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    uploadedPublicId = uploadResult.PublicId;
                    return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
                }

                var errorMsg = uploadResult.Error?.Message ?? "Unknown error";
                throw new Exception($"Upload thất bại: {errorMsg}");
            }
            catch (Exception)
            {
                // Rollback nếu upload thành công nhưng có lỗi sau đó
                if (!string.IsNullOrEmpty(uploadedPublicId))
                {
                    try { await DeleteImageAsync(uploadedPublicId); }
                    catch { /* Ignore rollback failure */ }
                }
                throw;
            }
        }

        /// <summary>
        /// Xóa ảnh trên Cloudinary theo PublicId.
        /// </summary>
        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return;

            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}