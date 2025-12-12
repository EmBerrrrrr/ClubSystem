using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file)
        {
            // 1. Kiểm tra file có tồn tại không
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("[PhotoService] File is null or empty");
                throw new ArgumentException("File không được để trống");
            }

            // 2. Kiểm tra dung lượng (giới hạn 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                Console.WriteLine($"[PhotoService] File size exceeds limit: {file.Length} bytes (max: {maxFileSize} bytes)");
                throw new ArgumentException($"Kích thước file vượt quá giới hạn 5MB. File hiện tại: {file.Length / 1024.0 / 1024.0:F2}MB");
            }

            // 3. Kiểm tra loại file (extension)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"[PhotoService] Invalid file extension: {fileExtension}");
                throw new ArgumentException($"Định dạng file không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}");
            }

            // 4. Kiểm tra MIME type để tránh hacker đổi tên file
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic", "image/gif" };
            if (string.IsNullOrEmpty(file.ContentType) || !allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                Console.WriteLine($"[PhotoService] Invalid MIME type: {file.ContentType}");
                throw new ArgumentException($"MIME type không hợp lệ. File phải là ảnh hợp lệ.");
            }

            Console.WriteLine($"[PhotoService] Starting upload: {file.FileName}, Size: {file.Length} bytes, Type: {file.ContentType}");

            string? uploadedPublicId = null;
            try
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "studentclub",
                    UniqueFilename = true,
                    Overwrite = false // Không overwrite để tránh xóa nhầm
                };

                Console.WriteLine($"[PhotoService] Uploading to Cloudinary folder: studentclub");
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                // 6. Kiểm tra lỗi Cloudinary
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    uploadedPublicId = uploadResult.PublicId;
                    Console.WriteLine($"[PhotoService] Upload successful! PublicId: {uploadResult.PublicId}, URL: {uploadResult.SecureUrl}");
                    return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
                }
                else
                {
                    var errorMsg = uploadResult.Error?.Message ?? "Unknown error";
                    Console.WriteLine($"[PhotoService] Upload failed! StatusCode: {uploadResult.StatusCode}, Error: {errorMsg}");
                    throw new Exception($"Upload thất bại: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                // Rollback: Xóa ảnh đã upload nếu có
                if (!string.IsNullOrEmpty(uploadedPublicId))
                {
                    try
                    {
                        Console.WriteLine($"[PhotoService] Rolling back: Deleting uploaded image {uploadedPublicId}");
                        await DeleteImageAsync(uploadedPublicId);
                    }
                    catch (Exception rollbackEx)
                    {
                        Console.WriteLine($"[PhotoService] Rollback failed: {rollbackEx.Message}");
                    }
                }

                Console.WriteLine($"[PhotoService] Exception during upload: {ex.Message}");
                Console.WriteLine($"[PhotoService] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return;

            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
