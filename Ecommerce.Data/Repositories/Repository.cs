using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly EcommerceEntities _context;
        private readonly DbSet<T> _dbSet;

        public Repository(EcommerceEntities context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public T GetById(int id) => _dbSet.Find(id);

        public IEnumerable<T> GetAll() => _dbSet.ToList();

        public void Add(T entity) => _dbSet.Add(entity);

        public void Update(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(T entity) => _dbSet.Remove(entity);

        public void Save() => _context.SaveChanges();

        /// <summary>Exposed for specialized repositories / service-layer composition queries.</summary>
        protected EcommerceEntities Context => _context;

        protected DbSet<T> DbSet => _dbSet;
    }
}
