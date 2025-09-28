using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // RefreshToken 관계 설정
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);
        
        // RefreshToken 인덱스
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
        
        // Seed data for testing
        modelBuilder.Entity<Document>().HasData(
            new Document 
            { 
                Id = 1, 
                Title = "Azure AD 가이드", 
                Content = "Azure AD 인증 설정 방법: 1. Azure Portal 접속 2. App Registration 생성 3. Client ID 및 Tenant ID 확인 4. Redirect URI 설정", 
                Category = "Authentication", 
                CreatedAt = DateTime.UtcNow, 
                UserId = 1 
            },
            new Document 
            { 
                Id = 2, 
                Title = "JWT 토큰 이해하기", 
                Content = "JWT는 Header.Payload.Signature 구조로 되어 있으며, Base64로 인코딩됩니다. Access Token은 짧은 수명, Refresh Token은 긴 수명을 가집니다.", 
                Category = "Security", 
                CreatedAt = DateTime.UtcNow, 
                UserId = 1 
            },
            new Document 
            { 
                Id = 3, 
                Title = "PKCE 플로우", 
                Content = "PKCE(Proof Key for Code Exchange)는 Code Verifier와 Code Challenge를 사용하여 Authorization Code 가로채기 공격을 방지합니다.", 
                Category = "Authentication", 
                CreatedAt = DateTime.UtcNow, 
                UserId = 1 
            },
            new Document 
            { 
                Id = 4, 
                Title = "OAuth 2.0 기본 개념", 
                Content = "OAuth 2.0은 인증 프로토콜로 Resource Owner, Client, Authorization Server, Resource Server의 4가지 역할로 구성됩니다.", 
                Category = "Security", 
                CreatedAt = DateTime.UtcNow, 
                UserId = 1 
            },
            new Document 
            { 
                Id = 5, 
                Title = "MSAL.js 사용법", 
                Content = "Microsoft Authentication Library는 Azure AD 인증을 쉽게 구현할 수 있게 해주는 JavaScript 라이브러리입니다.", 
                Category = "Development", 
                CreatedAt = DateTime.UtcNow, 
                UserId = 1 
            }
        );
    }
}
