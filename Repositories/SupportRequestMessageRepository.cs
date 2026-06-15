using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories.Repositories
{
    public class SupportRequestMessageRepository : ISupportRequestMessageRepository
    {
        private readonly AppDbContext _context;

        public SupportRequestMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SupportRequestMessage message)
        {
            await _context.Set<SupportRequestMessage>().AddAsync(message);
        }

        public async Task<List<SupportRequestMessage>> GetBySupportRequestIdAsync(int supportRequestId)
        {
            return await _context.Set<SupportRequestMessage>()
                .AsNoTracking()
                .Include(x => x.SenderUser)
                .Where(x => x.SupportRequestId == supportRequestId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
