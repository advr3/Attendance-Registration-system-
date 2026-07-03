using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using QRCoder;

namespace personal_attendanse_system.Globals
{
    public class Methods
    {
        public static async Task<bool> IsUserAdminAsync(ApplicationDbContext context, string userId)
        {
            return await context.Admins.AnyAsync(a => a.UserId == userId);
        }

        public static async Task<Admin> GetAdminAsync(ApplicationDbContext context, string userId)
        {
            return await context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public static async Task<bool> IsUserStaffAsync(ApplicationDbContext context, string userId, string groupId)
        {
            return await context.GroupStaffs.AnyAsync(a => a.UserId == userId && a.GroupId.ToString() == groupId);
        }

        public static async Task<GroupStaff> GetStaffAsync(ApplicationDbContext context, string userId, string groupId)
        {
            return await context.GroupStaffs.FirstOrDefaultAsync(a => a.UserId == userId && a.GroupId.ToString() == groupId);
        }

        public static async Task<bool> IsUserMemberAsync(ApplicationDbContext context, string userId, string groupId)
        {
            return await context.GroupMembers.AnyAsync(a => a.UserId == userId && a.GroupId.ToString() == groupId);
        }

        public static async Task<GroupStaff> GetMemberAsync(ApplicationDbContext context, string userId, string groupId)
        {
            return await context.GroupStaffs.FirstOrDefaultAsync(a => a.UserId == userId && a.GroupId.ToString() == groupId);
        }


        public static string GenerateQRCodeAsBase64(string linkUrl)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(linkUrl, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new PngByteQRCode(qrCodeData))
            {
                var qrCodeBytes = qrCode.GetGraphic(20);
                return Convert.ToBase64String(qrCodeBytes);
            }
        }

        public static string GenerateQrCodeImage(string qrData)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeBytes = qrCode.GetGraphic(20);
                        var base64String = Convert.ToBase64String(qrCodeBytes);
                        return $"data:image/png;base64,{base64String}";
                    }
                }
            }
        }

        public static string GenerateCronExpression(string[] days, DateTime startTime)
        {
            var dayNumbers = days.Select(d => ((int)Enum.Parse(typeof(DayOfWeek), d)).ToString());
            var dayOfWeekPart = string.Join(",", dayNumbers);

            var hourPart = startTime.Hour.ToString();
            var minutePart = startTime.Minute.ToString();

            return $"{minutePart} {hourPart} * * {dayOfWeekPart}";
        }
    }
}
