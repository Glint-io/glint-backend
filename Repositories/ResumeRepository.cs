using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories;

public class ResumeRepository(AppDBContext db) : IResumeRepository
{
    public async Task<Resume> AddAsync(Resume resume)
    {
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();
        return resume;
    }

    public async Task<Resume?> GetByIdAsync(Guid id) =>
        await db.Resumes.FindAsync(id);
}