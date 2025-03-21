using HelpdeskTicketing.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Repositories.Interfaces
{
    public interface ITicketRepository
    {
        Task<Ticket> GetByIdAsync(int id, bool includeRelated = true);
        Task<IEnumerable<Ticket>> GetAllAsync();
        Task<IEnumerable<Ticket>> FindAsync(Expression<Func<Ticket, bool>> predicate);
        Task<int> CountAsync(Expression<Func<Ticket, bool>> predicate = null);
        Task AddAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
        Task DeleteAsync(int id);
        Task<IEnumerable<Ticket>> GetPagedAsync(int page, int pageSize, Expression<Func<Ticket, bool>> filter = null, string sortBy = null, bool ascending = true);
        Task<IEnumerable<Ticket>> GetAssignedToUserAsync(string userId);
        Task<IEnumerable<Ticket>> GetRequestedByUserAsync(string userId);
        Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
        Task<IEnumerable<Ticket>> GetOverdueAsync();
    }
}
