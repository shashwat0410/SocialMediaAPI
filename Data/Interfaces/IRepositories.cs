using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Data.Interfaces
{
    // ── Generic Repository ─────────────────────────────────────
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        T Update(T entity);
        void Delete(T entity);
    }

    // ── Post Repository ────────────────────────────────────────
    public interface IPostRepository : IRepository<Post>
    {
        Task<PagedResult<Post>> GetPostsAsync(PaginationParams pagination, string? currentUserId = null);
        Task<PagedResult<Post>> GetUserPostsAsync(string userId, PaginationParams pagination);
        Task<PagedResult<Post>> GetFeedAsync(string userId, PaginationParams pagination);
        Task<Post?> GetPostWithDetailsAsync(int postId);
        Task<bool> IsLikedByUserAsync(int postId, string userId);
    }

    // ── Comment Repository ─────────────────────────────────────
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<PagedResult<Comment>> GetPostCommentsAsync(int postId, PaginationParams pagination);
        Task<Comment?> GetCommentWithRepliesAsync(int commentId);
    }

    // ── Like Repository ────────────────────────────────────────
    public interface ILikeRepository : IRepository<Like>
    {
        Task<Like?> GetLikeAsync(int postId, string userId);
        Task<int> GetLikesCountAsync(int postId);
    }

    // ── Follow Repository ──────────────────────────────────────
    public interface IFollowRepository : IRepository<Follow>
    {
        Task<Follow?> GetFollowAsync(string followerId, string followingId);
        Task<PagedResult<ApplicationUser>> GetFollowersAsync(string userId, PaginationParams pagination);
        Task<PagedResult<ApplicationUser>> GetFollowingAsync(string userId, PaginationParams pagination);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<int> GetFollowersCountAsync(string userId);
        Task<int> GetFollowingCountAsync(string userId);
    }

    // ── User Repository ────────────────────────────────────────
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByUsernameAsync(string username);
        Task<PagedResult<ApplicationUser>> SearchUsersAsync(PaginationParams pagination);
        Task<bool> UpdateAsync(ApplicationUser user);
    }

    // ── Refresh Token Repository ───────────────────────────────
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken token);
        Task RevokeAsync(string token, string? replacedByToken = null);
        Task RevokeAllUserTokensAsync(string userId);
    }

    // ── Unit of Work ───────────────────────────────────────────
    public interface IUnitOfWork : IDisposable
    {
        IPostRepository Posts { get; }
        ICommentRepository Comments { get; }
        ILikeRepository Likes { get; }
        IFollowRepository Follows { get; }
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        Task<int> SaveChangesAsync();
    }
}
