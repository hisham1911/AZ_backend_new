using Microsoft.EntityFrameworkCore;
using az_backend_new.Data;
using az_backend_new.Models;
using az_backend_new.DTOs;

namespace az_backend_new.Repositories
{
    public interface ITraineeRepository
    {
        Task<Trainee?> GetByIdAsync(int id);
        Task<Trainee?> GetByIdWithCertificatesAsync(int id);
        Task<Trainee?> GetBySerialNumberAsync(string serialNumber);
        Task<Trainee?> GetBySerialNumberWithCertificatesAsync(string serialNumber);
        Task<PagedResult<Trainee>> GetAllAsync(int page, int pageSize);
        Task<PagedResult<Trainee>> GetAllWithCertificatesAsync(int page, int pageSize);
        Task<List<Trainee>> SearchAsync(TraineeSearchDto searchDto);
        Task<Trainee> CreateAsync(Trainee trainee);
        Task<Trainee> UpdateAsync(Trainee trainee);
        Task<bool> DeleteAsync(int id);
        Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null);
        Task DeleteAllAsync();
        
        // Certificate operations
        Task<Certificate?> GetCertificateByIdAsync(int id);
        Task<Certificate> AddCertificateAsync(int traineeId, Certificate certificate);
        Task<Certificate> UpdateCertificateAsync(Certificate certificate);
        Task<bool> DeleteCertificateAsync(int certificateId);
        Task<bool> TraineeHasCertificateWithMethodAsync(int traineeId, ServiceMethod method, int? excludeCertId = null);
    }

    public class TraineeRepository : ITraineeRepository
    {
        private readonly AzDbContext _context;

        public TraineeRepository(AzDbContext context)
        {
            _context = context;
        }

        public async Task<Trainee?> GetByIdAsync(int id)
        {
            return await _context.Trainees.FindAsync(id);
        }

        public async Task<Trainee?> GetByIdWithCertificatesAsync(int id)
        {
            return await _context.Trainees
                .Include(t => t.Certificates)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Trainee?> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Trainees
                .FirstOrDefaultAsync(t => t.SerialNumber == serialNumber);
        }

        public async Task<Trainee?> GetBySerialNumberWithCertificatesAsync(string serialNumber)
        {
            return await _context.Trainees
                .Include(t => t.Certificates)
                .FirstOrDefaultAsync(t => t.SerialNumber == serialNumber);
        }

        public async Task<PagedResult<Trainee>> GetAllAsync(int page, int pageSize)
        {
            var query = _context.Trainees.AsQueryable();
            
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Trainee>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<Trainee>> GetAllWithCertificatesAsync(int page, int pageSize)
        {
            var query = _context.Trainees
                .Include(t => t.Certificates)
                .AsQueryable();
            
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Trainee>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Trainee>> SearchAsync(TraineeSearchDto searchDto)
        {
            var query = _context.Trainees
                .Include(t => t.Certificates)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchDto.SerialNumber))
            {
                var searchTerm = searchDto.SerialNumber.ToLower();
                query = query.Where(t => t.SerialNumber.ToLower().Contains(searchTerm) || 
                                        t.SerialNumber.ToLower().StartsWith(searchTerm));
            }

            if (!string.IsNullOrEmpty(searchDto.PersonName))
            {
                query = query.Where(t => t.PersonName.ToLower().Contains(searchDto.PersonName.ToLower()));
            }

            if (searchDto.ServiceMethod.HasValue)
            {
                query = query.Where(t => t.Certificates.Any(c => c.ServiceMethod == searchDto.ServiceMethod.Value));
            }

            if (searchDto.HasExpiredCertificates.HasValue)
            {
                var now = DateTime.UtcNow;
                if (searchDto.HasExpiredCertificates.Value)
                {
                    query = query.Where(t => t.Certificates.Any(c => c.ExpiryDate < now));
                }
                else
                {
                    query = query.Where(t => t.Certificates.All(c => c.ExpiryDate >= now));
                }
            }

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();
        }

        public async Task<Trainee> CreateAsync(Trainee trainee)
        {
            trainee.CreatedAt = DateTime.UtcNow;
            trainee.UpdatedAt = DateTime.UtcNow;
            
            _context.Trainees.Add(trainee);
            await _context.SaveChangesAsync();
            return trainee;
        }

        public async Task<Trainee> UpdateAsync(Trainee trainee)
        {
            trainee.UpdatedAt = DateTime.UtcNow;
            
            _context.Trainees.Update(trainee);
            await _context.SaveChangesAsync();
            return trainee;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trainee = await _context.Trainees
                .Include(t => t.Certificates)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (trainee == null)
                return false;

            if (trainee.Certificates.Any())
            {
                _context.Certificates.RemoveRange(trainee.Certificates);
            }
            _context.Trainees.Remove(trainee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null)
        {
            var query = _context.Trainees.Where(t => t.SerialNumber == serialNumber);
            
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task DeleteAllAsync()
        {
            _context.Certificates.RemoveRange(_context.Certificates);
            _context.Trainees.RemoveRange(_context.Trainees);
            await _context.SaveChangesAsync();
        }

        // Certificate operations
        public async Task<Certificate?> GetCertificateByIdAsync(int id)
        {
            return await _context.Certificates
                .Include(c => c.Trainee)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Certificate> AddCertificateAsync(int traineeId, Certificate certificate)
        {
            certificate.TraineeId = traineeId;
            certificate.CreatedAt = DateTime.UtcNow;
            certificate.UpdatedAt = DateTime.UtcNow;
            
            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<Certificate> UpdateCertificateAsync(Certificate certificate)
        {
            certificate.UpdatedAt = DateTime.UtcNow;
            
            _context.Certificates.Update(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<bool> DeleteCertificateAsync(int certificateId)
        {
            var certificate = await _context.Certificates.FindAsync(certificateId);
            if (certificate == null)
                return false;

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TraineeHasCertificateWithMethodAsync(int traineeId, ServiceMethod method, int? excludeCertId = null)
        {
            var query = _context.Certificates
                .Where(c => c.TraineeId == traineeId && c.ServiceMethod == method);
            
            if (excludeCertId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCertId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
