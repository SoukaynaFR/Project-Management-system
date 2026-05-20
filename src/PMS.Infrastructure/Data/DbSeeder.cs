using BCrypt.Net;
using PMS.Domain.Entities;
using PMS.Domain.Enums;

namespace PMS.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Users.Any())
            return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Youssef",
            LastName = "El Idrissi",
            Email = "admin@projectflow.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = Role.Admin,
            IsActive = true
        };

        var manager = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Nadia",
            LastName = "Benali",
            Email = "manager@projectflow.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager1234"),
            Role = Role.Manager,
            IsActive = true
        };

        var developer = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Mehdi",
            LastName = "Tazi",
            Email = "dev@projectflow.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("dev123"),
            Role = Role.Developer,
            IsActive = true
        };

        var developer2 = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Salma",
            LastName = "Cherkaoui",
            Email = "dev2@projectflow.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("dev123"),
            Role = Role.Developer,
            IsActive = true
        };

        context.Users.AddRange(admin, manager, developer, developer2);
        context.SaveChanges();
    }
}