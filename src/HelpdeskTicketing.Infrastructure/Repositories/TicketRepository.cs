using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Data;
using HelpdeskTicketing.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket> GetByIdAsync(int id, bool includeRelated = true)
        {
            if (includeRelated)
            {
                return await _context.Tickets
                    .Include(t => t.Requester)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Author)
                    .Include(t => t.Attachments)
                    .Include(t => t.History)
                        .ThenInclude(h => h.ChangedBy)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            
            return await _context.Tickets.FindAsync(id);
        }

        public async Task<IEnumerable<Ticket>> GetAllAsync()
        {
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> FindAsync(Expression<Func<Ticket, bool>> predicate)
        {
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<Ticket, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return await _context.Tickets.CountAsync();
            }
            
            return await _context.Tickets.CountAsync(predicate);
        }

        public async Task AddAsync(Ticket ticket)
        {
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Ticket>> GetPagedAsync(int page, int pageSize, Expression<Func<Ticket, bool>> filter = null, string sortBy = null, bool ascending = true)
        {
            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                PropertyInfo prop = typeof(Ticket).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                
                if (prop != null)
                {
                    var parameter = Expression.Parameter(typeof(Ticket), "x");
                    var property = Expression.Property(parameter, prop);
                    var lambda = Expression.Lambda(property, parameter);

                    string methodName = ascending ? "OrderBy" : "OrderByDescending";
                    Type[] types = new Type[] { typeof(Ticket), prop.PropertyType };
                    var methodCallExpression = Expression.Call(typeof(Queryable), methodName, types, query.Expression, lambda);

                    query = query.Provider.CreateQuery<Ticket>(methodCallExpression);
                }
                else
                {
                    // Default sort by created date if property not found
                    query = ascending ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt);
                }
            }
            else
            {
                // Default sort by created date
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetAssignedToUserAsync(string userId)
        {
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .Where(t => t.AssignedToId == userId && t.Status != TicketStatus.Closed)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetRequestedByUserAsync(string userId)
        {
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .Where(t => t.RequesterId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status)
        {
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetOverdueAsync()
        {
            var now = DateTime.UtcNow;
            
            return await _context.Tickets
                .Include(t => t.Requester)
                .Include(t => t.AssignedTo)
                .Where(t => t.Status != TicketStatus.Resolved && 
                           t.Status != TicketStatus.Closed &&
                           t.CreatedAt.AddMinutes(t.EstimatedResolutionTimeInMinutes ?? 0) < now)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
