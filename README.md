# SocialMedia API — .NET 8 Web API

A production-grade **Social Media REST API** built with ASP.NET Core 8, featuring JWT authentication with Refresh Tokens, Role-Based Authorization, Repository + Unit of Work pattern, Global Exception Handling, Serilog logging, and full Swagger documentation.

---

## ✨ Feature Set

| Feature | Implementation |
|---|---|
| **JWT Auth** | Access Token (15 min) + Refresh Token (7 days) rotation |
| **Role-Based Auth** | `Admin` / `User` roles with `[Authorize(Roles="Admin")]` |
| **Refresh Token Rotation** | Old token revoked, new token issued on every refresh |
| **Global Exception Handling** | Middleware catches all unhandled exceptions + structured JSON errors |
| **Serilog Logging** | Logs to Console + rolling daily files in `/logs` |
| **Pagination** | All list endpoints support `page`, `pageSize`, `search`, `sortBy`, `sortOrder` |
| **Soft Delete** | Posts and Comments are soft-deleted (IsDeleted flag) |
| **Swagger UI** | Full OpenAPI docs with JWT Bearer authorization built-in |
| **Unit of Work** | All repositories coordinated through a single UoW transaction |

---

## 🏗️ Architecture

```
SocialMediaAPI/
│
├── Controllers/
│   └── Controllers.cs        # Auth, Posts, Comments, Users, Admin
│
├── Models/
│   └── Models.cs             # ApplicationUser, Post, Comment, Like, Follow, RefreshToken
│
├── DTOs/
│   └── Dtos.cs               # All Request/Response DTOs + PagedResult + ApiResponse<T>
│
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Interfaces/
│   │   └── IRepositories.cs  # IRepository<T>, IPostRepo, ICommentRepo, IUnitOfWork...
│   └── Repositories/
│       └── Repositories.cs   # All concrete implementations + UnitOfWork
│
├── Services/
│   ├── TokenService.cs       # JWT generation, refresh token, claims extraction
│   ├── AuthService.cs        # Register, Login, Refresh, Logout
│   └── Services.cs           # PostService, CommentService, UserService
│
├── Middleware/
│   └── GlobalExceptionMiddleware.cs  # Catches all unhandled exceptions
│
├── Configurations/
│   └── JwtSettings.cs        # Strongly-typed JWT config
│
└── Program.cs                # DI, JWT, Swagger, CORS, Serilog, Role seeding
```

---

## 🔗 API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | ❌ | Register new user |
| POST | `/api/auth/login` | ❌ | Login, get JWT + refresh token |
| POST | `/api/auth/refresh-token` | ❌ | Rotate refresh token |
| POST | `/api/auth/logout` | ✅ | Revoke all refresh tokens |

### Posts
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/posts` | ❌ | Get all posts (paginated, searchable) |
| GET | `/api/posts/feed` | ✅ | Personalized feed from followed users |
| GET | `/api/posts/{id}` | ❌ | Get post by ID |
| POST | `/api/posts` | ✅ | Create post |
| PUT | `/api/posts/{id}` | ✅ | Update own post |
| DELETE | `/api/posts/{id}` | ✅ | Soft delete own post |
| POST | `/api/posts/{id}/like` | ✅ | Toggle like/unlike |
| GET | `/api/posts/{id}/comments` | ❌ | Get comments (paginated) |
| POST | `/api/posts/{id}/comments` | ✅ | Add comment / reply |

### Users
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/users/search` | ❌ | Search users |
| GET | `/api/users/{username}` | ❌ | Get user profile |
| GET | `/api/users/{userId}/posts` | ❌ | Get user's posts |
| PUT | `/api/users/me` | ✅ | Update own profile |
| POST | `/api/users/{userId}/follow` | ✅ | Toggle follow/unfollow |
| GET | `/api/users/{userId}/followers` | ❌ | Get followers list |
| GET | `/api/users/{userId}/following` | ❌ | Get following list |

### Comments
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| PUT | `/api/comments/{id}` | ✅ | Update own comment |
| DELETE | `/api/comments/{id}` | ✅ | Soft delete own comment |

### Admin (Role = Admin)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/admin/users` | ✅ Admin | Get all users |
| DELETE | `/api/admin/posts/{id}` | ✅ Admin | Delete any post |

---

## 🗄️ Database Tables

| Table | Purpose |
|---|---|
| `AspNetUsers` | User accounts (extended with FullName, Bio, ProfilePic) |
| `AspNetRoles` | Admin, User roles |
| `AspNetUserRoles` | User-to-role mapping |
| `AspNetUserClaims` | User claims |
| `AspNetRoleClaims` | Role claims |
| `AspNetUserLogins` | External providers |
| `AspNetUserTokens` | Auth tokens |
| `Posts` | Posts with soft delete |
| `Comments` | Nested comments (self-referencing) with soft delete |
| `Likes` | Unique user-post likes |
| `Follows` | User follow relationships |
| `RefreshTokens` | JWT refresh tokens with revocation |

**Total: 12 tables**

---

## 🚀 Getting Started

```bash
# 1. Install EF CLI (if not already)
dotnet tool install --global dotnet-ef

# 2. Restore packages
dotnet restore

# 3. Apply database migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# 4. Run
dotnet run
```

Open `https://localhost:5001` — Swagger UI loads automatically.

**Default Admin:** `admin@socialmedia.com` / `Admin@123`

---

## 🔐 JWT Flow

```
1. POST /api/auth/login  →  { accessToken, refreshToken }
2. Use accessToken in: Authorization: Bearer {token}
3. Token expires after 15 min
4. POST /api/auth/refresh-token  →  new { accessToken, refreshToken }
5. POST /api/auth/logout  →  all refresh tokens revoked
```

---

## 💡 Resume Talking Points

- "Built a production-grade REST API using ASP.NET Core 8 with JWT Bearer authentication and Refresh Token rotation"
- "Implemented Repository Pattern + Unit of Work for clean data access layer with EF Core 8"
- "Designed Role-Based Authorization with Admin/User roles using ASP.NET Core Identity"
- "Created Global Exception Handling middleware returning consistent JSON error responses"
- "Integrated Serilog structured logging to Console and rolling file sinks"
- "Added server-side Pagination, Search, and Sorting on all list endpoints"
- "Documented all 20+ endpoints using Swagger/OpenAPI with JWT authorization support"
- "Implemented soft delete pattern for posts and comments to preserve data integrity"
