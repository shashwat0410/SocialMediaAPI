using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data.Interfaces;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Data.Repositories
{
    // ── Generic Repository ─────────────────────────────────────
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public async Task<T> AddAsync(T entity) { var e = await _dbSet.AddAsync(entity); return e.Entity; }
        public T Update(T entity) => _dbSet.Update(entity).Entity;
        public void Delete(T entity) => _dbSet.Remove(entity);
    }

    // ── Post Repository ────────────────────────────────────────
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(ApplicationDbContext context) : base(context) { }

        public async Task<PagedResult<Post>> GetPostsAsync(PaginationParams p, string? currentUserId = null)
        {
            var query = _context.Posts
                .Include(x => x.User)
                .Include(x => x.Likes)
                .Include(x => x.Comments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(p.Search))
                query = query.Where(x => x.Content.Contains(p.Search));

            query = p.SortBy.ToLower() switch
            {
                "likes"    => p.SortOrder == "asc" ? query.OrderBy(x => x.Likes.Count) : query.OrderByDescending(x => x.Likes.Count),
                "createdat"=> p.SortOrder == "asc" ? query.OrderBy(x => x.CreatedAt)   : query.OrderByDescending(x => x.CreatedAt),
                _          => query.OrderByDescending(x => x.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<Post> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<PagedResult<Post>> GetUserPostsAsync(string userId, PaginationParams p)
        {
            var query = _context.Posts
                .Include(x => x.User)
                .Include(x => x.Likes)
                .Include(x => x.Comments)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<Post> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<PagedResult<Post>> GetFeedAsync(string userId, PaginationParams p)
        {
            // Feed = posts from users you follow + your own posts
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            followingIds.Add(userId);

            var query = _context.Posts
                .Include(x => x.User)
                .Include(x => x.Likes)
                .Include(x => x.Comments)
                .Where(x => followingIds.Contains(x.UserId))
                .OrderByDescending(x => x.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<Post> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<Post?> GetPostWithDetailsAsync(int postId) =>
            await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes).ThenInclude(l => l.User)
                .Include(p => p.Comments.Where(c => c.ParentCommentId == null))
                    .ThenInclude(c => c.User)
                .Include(p => p.Comments.Where(c => c.ParentCommentId == null))
                    .ThenInclude(c => c.Replies).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

        public async Task<bool> IsLikedByUserAsync(int postId, string userId) =>
            await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
    }

    // ── Comment Repository ─────────────────────────────────────
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<PagedResult<Comment>> GetPostCommentsAsync(int postId, PaginationParams p)
        {
            var query = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<Comment> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<Comment?> GetCommentWithRepliesAsync(int commentId) =>
            await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);
    }

    // ── Like Repository ────────────────────────────────────────
    public class LikeRepository : Repository<Like>, ILikeRepository
    {
        public LikeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Like?> GetLikeAsync(int postId, string userId) =>
            await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        public async Task<int> GetLikesCountAsync(int postId) =>
            await _context.Likes.CountAsync(l => l.PostId == postId);
    }

    // ── Follow Repository ──────────────────────────────────────
    public class FollowRepository : Repository<Follow>, IFollowRepository
    {
        public FollowRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Follow?> GetFollowAsync(string followerId, string followingId) =>
            await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        public async Task<PagedResult<ApplicationUser>> GetFollowersAsync(string userId, PaginationParams p)
        {
            var query = _context.Follows
                .Where(f => f.FollowingId == userId)
                .Select(f => f.Follower)
                .OrderBy(u => u.FullName);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<ApplicationUser> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<PagedResult<ApplicationUser>> GetFollowingAsync(string userId, PaginationParams p)
        {
            var query = _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingUser)
                .OrderBy(u => u.FullName);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<ApplicationUser> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<bool> IsFollowingAsync(string followerId, string followingId) =>
            await _context.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        public async Task<int> GetFollowersCountAsync(string userId) =>
            await _context.Follows.CountAsync(f => f.FollowingId == userId);

        public async Task<int> GetFollowingCountAsync(string userId) =>
            await _context.Follows.CountAsync(f => f.FollowerId == userId);
    }

    // ── User Repository ────────────────────────────────────────
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) => _context = context;

        public async Task<ApplicationUser?> GetByIdAsync(string id) =>
            await _context.Users.FindAsync(id);

        public async Task<ApplicationUser?> GetByUsernameAsync(string username) =>
            await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        public async Task<PagedResult<ApplicationUser>> SearchUsersAsync(PaginationParams p)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(p.Search))
                query = query.Where(u => u.UserName!.Contains(p.Search) || u.FullName.Contains(p.Search));

            query = query.OrderBy(u => u.FullName);

            var total = await query.CountAsync();
            var items = await query.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToListAsync();

            return new PagedResult<ApplicationUser> { Items = items, TotalCount = total, Page = p.Page, PageSize = p.PageSize };
        }

        public async Task<bool> UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }
    }

    // ── Refresh Token Repository ───────────────────────────────
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;
        public RefreshTokenRepository(ApplicationDbContext context) => _context = context;

        public async Task<RefreshToken?> GetByTokenAsync(string token) =>
            await _context.RefreshTokens.Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAsync(string token, string? replacedByToken = null)
        {
            var rt = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
            if (rt != null)
            {
                rt.RevokedAt = DateTime.UtcNow;
                rt.ReplacedByToken = replacedByToken;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ToListAsync();

            foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // ── Unit of Work ───────────────────────────────────────────
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IPostRepository Posts { get; }
        public ICommentRepository Comments { get; }
        public ILikeRepository Likes { get; }
        public IFollowRepository Follows { get; }
        public IUserRepository Users { get; }
        public IRefreshTokenRepository RefreshTokens { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Posts = new PostRepository(context);
            Comments = new CommentRepository(context);
            Likes = new LikeRepository(context);
            Follows = new FollowRepository(context);
            Users = new UserRepository(context);
            RefreshTokens = new RefreshTokenRepository(context);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();
    }
}
