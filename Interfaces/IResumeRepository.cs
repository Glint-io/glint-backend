using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IResumeRepository
{
    Task<Resume> AddAsync(Resume resume);
    Task<Resume?> GetByIdAsync(Guid id);
}