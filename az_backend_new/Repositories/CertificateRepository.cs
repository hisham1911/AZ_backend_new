using Microsoft.EntityFrameworkCore;
using az_backend_new.Data;
using az_backend_new.Models;
using az_backend_new.DTOs;

namespace az_backend_new.Repositories
{
    public interface ICertificateRepository
    {
        Task<Certificate?> GetByIdAsync(int id);
        Task<Certificate?> GetBySerialNumberAsync(string serialNumber);
        Task<PagedResult<Certificate>> GetAllAsync(int page, int pageSize);
        Task<List<Certificate>> SearchAsync(CertificateSearchDto searchDto);
        Task<Certificate> CreateAsync(Certificate certificate);
        Task<Certificate> UpdateAsync(Certificate certificate);
        Task<bool> DeleteAsync(int id);
        Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null);
        Task<List<Certificate>> GetExpiringCertificatesAsync(int daysFromNow = 30);
    }

    public class CertificateRepository : ICertificateRepository
    {
        private readonly AzDbContext _context;

        public CertificateRepository(AzDbContext context)
        {
            _context = context;
        }

        public async Task<Certificate?> GetByIdAsync(int id)
        {
            return await _context.Certificates.FindAsync(id);
        }

        public async Task<Certificate?> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Certificates
                .FirstOrDefaultAsync(c => c.SerialNumber == serialNumber);
        }

        public async Task<PagedResult<Certificate>> GetAllAsync(int page, int pageSize)
        {
            var query = _context.Certificates.AsQueryable();
            
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Certificate>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Certificate>> SearchAsync(CertificateSearchDto searchDto)
        {
            var query = _context.Certificates.AsQueryable();

            if (!string.IsNullOrEmpty(searchDto.SerialNumber))
            {
                query = query.Where(c => c.SerialNumber == searchDto.SerialNumber);
            }

            if (!string.IsNullOrEmpty(searchDto.PersonName))
            {
                query = query.Where(c => c.PersonName.ToLower().Contains(searchDto.PersonName.ToLower()));
            }

            if (searchDto.ServiceMethod.HasValue)
            {
                query = query.Where(c => c.ServiceMethod == searchDto.ServiceMethod.Value);
            }

            if (searchDto.Expired.HasValue)
            {
                var now = DateTime.UtcNow;
                if (searchDto.Expired.Value)
                {
                    query = query.Where(c => c.ExpiryDate < now);
                }
                else
                {
                    query = query.Where(c => c.ExpiryDate >= now);
                }
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();
        }

        public async Task<Certificate> CreateAsync(Certificate certificate)
        {
            certificate.CreatedAt = DateTime.UtcNow;
            certificate.UpdatedAt = DateTime.UtcNow;
            
            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<Certificate> UpdateAsync(Certificate certificate)
        {
            certificate.UpdatedAt = DateTime.UtcNow;
            
            _context.Certificates.Update(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate == null)
                return false;

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null)
        {
            var query = _context.Certificates.Where(c => c.SerialNumber == serialNumber);
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Certificate>> GetExpiringCertificatesAsync(int daysFromNow = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(daysFromNow);
            
            return await _context.Certificates
                .Where(c => c.ExpiryDate <= cutoffDate && c.ExpiryDate >= DateTime.UtcNow)
                .ToListAsync();
        }
    }
}