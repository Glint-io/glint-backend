using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories;

public class ResumeRepository(AppDBContext db) : IResumeRepository
{
    // add a new resume
    public async Task<Resume> AddAsync(Resume resume)
    {
        db.Resumes.Add(resume);
        await db.SaveChangesAsync();
        return resume;
    }

    // get a resume by its ID, returning null if not found
    public async Task<Resume?> GetByIdAsync(Guid id) =>
        await db.Resumes.FindAsync(id);

    // count the number of resumes a user has uploaded, used for enforcing limits and providing user statistics
    public async Task<int> CountByUserIdAsync(Guid userId) =>
        await db.Resumes.CountAsync(r => r.UserId == userId);

    public async Task<List<Resume>> GetAllByUserIdAsync(Guid userId) =>
    await db.Resumes
        .Where(r => r.UserId == userId)
        .OrderByDescending(r => r.UploadedAt)
        .Select(r => new Resume { Id = r.Id, FileName = r.FileName, UploadedAt = r.UploadedAt })
        .ToListAsync();

    // delete a resume
    public async Task DeleteAsync(Resume resume)
    {
        db.Resumes.Remove(resume);
        await db.SaveChangesAsync();
    }
}