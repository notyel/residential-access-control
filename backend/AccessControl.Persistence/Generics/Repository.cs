using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Persistence.Generics
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }
        public IQueryable<T> GetQueryable()
        {
            return _context.Set<T>().AsQueryable();
        }
        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }
        public void RemoveRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
