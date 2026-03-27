using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IResumeRepository
{
    // Methods for managing resumes in the database, e.g. adding, retrieving, counting, and deleting resumes.
    Task<Resume> AddAsync(Resume resume);
    Task<Resume?> GetByIdAsync(Guid id);
    Task<List<Resume>> GetAllByUserIdAsync(Guid userId);
    Task<int> CountByUserIdAsync(Guid userId);
    Task DeleteAsync(Resume resume);
}