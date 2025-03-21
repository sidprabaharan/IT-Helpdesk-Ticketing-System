using HelpdeskTicketing.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<TicketHistory> TicketHistory { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<TeamCategory> TeamCategories { get; set; }
        public DbSet<SLA> SLAs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Ticket>()
                .HasOne(t => t.Requester)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<TicketComment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TicketAttachment>()
                .HasOne(a => a.UploadedBy)
                .WithMany(u => u.Uploads)
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TicketHistory>()
                .HasOne(h => h.ChangedBy)
                .WithMany(u => u.Changes)
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for better performance
            builder.Entity<Ticket>()
                .HasIndex(t => t.Status);

            builder.Entity<Ticket>()
                .HasIndex(t => t.Priority);

            builder.Entity<Ticket>()
                .HasIndex(t => t.AssignedToId);

            builder.Entity<Ticket>()
                .HasIndex(t => t.CreatedAt);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Set timestamp fields
            var entries = ChangeTracker.Entries();
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.Entity is Ticket ticket && entry.State == EntityState.Modified)
                {
                    ticket.LastUpdatedAt = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}

// HelpdeskTicketing.Infrastructure/Repositories/Interfaces/ITicketRepository.cs
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

// HelpdeskTicketing.Infrastructure/Repositories/TicketRepository.cs
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
