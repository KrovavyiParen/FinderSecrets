using Microsoft.EntityFrameworkCore;
using Backend.Models;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class DatabaseService
    {
        private readonly AppDbContext _context;

        public DatabaseService(AppDbContext context)
        {
            _context = context;
        }

        // Запись запроса сканирования
        public async Task<int> SaveScanRequestAsync(ScanRequestEntity request)
        {
            _context.ScanRequests.Add(request);
            await _context.SaveChangesAsync();
            return request.Id;
        }

        // Запись найденных секретов
        public async Task SaveFoundSecretsAsync(List<FoundSecret> secrets)
        {
            if (secrets.Any())
            {
                await _context.FoundSecrets.AddRangeAsync(secrets);
                await _context.SaveChangesAsync();
            }
        }

        // Получение истории сканирований
        public async Task<List<ScanHistory>> GetScanHistoryAsync(int userId)
        {
            return await _context.ScanHistory
                .Where(sh => sh.UserId == userId)
                .OrderByDescending(sh => sh.ScannedAt)
                .ToListAsync();
        }

        // Обновление статистики после сканирования
        public async Task UpdateScanStatisticsAsync(int requestId, int secretsFound, int scanDuration)
        {
            var request = await _context.ScanRequests.FindAsync(requestId);
            if (request != null)
            {
                request.SecretsCount = secretsFound;
                request.ScanDuration = scanDuration;
                await _context.SaveChangesAsync();
            }
        }
        // Получение статистики по пользователю
        public async Task<UserStatisticsDto> GetUserStatisticsAsync(int userId)
        {
            var totalScans = await _context.ScanHistory
                .Where(sh => sh.UserId == userId)
                .CountAsync();

            var totalSecrets = await _context.ScanHistory
                .Where(sh => sh.UserId == userId)
                .SumAsync(sh => sh.SecretsFound);

            var lastScan = await _context.ScanHistory
                .Where(sh => sh.UserId == userId)
                .OrderByDescending(sh => sh.ScannedAt)
                .FirstOrDefaultAsync();

            return new UserStatisticsDto
            {
                TotalScans = totalScans,
                TotalSecretsFound = totalSecrets,
                LastScanDate = lastScan?.ScannedAt
            };
        }

    }
    public class UserStatisticsDto
    {
        public int TotalScans { get; set; }
        public int TotalSecretsFound { get; set; }
        public DateTime? LastScanDate { get; set; }
    }
}