# Secure API - .NET 8 with Identity & JWT

A production-ready ASP.NET Core 8 Web API template with Identity and JWT authentication following best practices.

## Features

- ✅ ASP.NET Core 8 Web API
- ✅ ASP.NET Core Identity for user management
- ✅ JWT Bearer authentication
- ✅ Refresh token support
- ✅ Role-based authorization
- ✅ Clean architecture with services layer
- ✅ Entity Framework Core with SQL Server
- ✅ Swagger/OpenAPI documentation with JWT support
- ✅ Comprehensive error handling
- ✅ Password security with Identity validators
- ✅ CORS configuration
- ✅ Structured DTOs and responses

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 / VS Code / Rider

## Getting Started

### 1. Update Connection String

Edit `appsettings.json` and update the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SecureApiDb;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

### 2. Update JWT Settings

⚠️ **IMPORTANT**: Change the `SecretKey` in `appsettings.json` for production:

```json
"JwtSettings": {
  "SecretKey": "YOUR-SUPER-SECRET-KEY-AT-LEAST-32-CHARACTERS",
  "Issuer": "YourIssuer",
  "Audience": "YourAudience",
  "ExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

### 3. Create Database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`
- Swagger UI: `https://localhost:7xxx/swagger`

## API Endpoints

### Authentication

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "userName": "username"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

Response:
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "accessToken": "eyJhbGci...",
    "refreshToken": "3x4mpl3...",
    "expiresAt": "2024-01-01T12:00:00Z"
  }
}
```

#### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "accessToken": "expired_token",
  "refreshToken": "refresh_token"
}
```

#### Revoke Token (Logout)
```http
POST /api/auth/revoke-token
Authorization: Bearer {access_token}
```

#### Get Current User
```http
GET /api/auth/me
Authorization: Bearer {access_token}
```

### Protected Endpoints

#### Get Weather Forecast (Requires Authentication)
```http
GET /api/weatherforecast
Authorization: Bearer {access_token}
```

#### Get Admin Data (Requires Admin Role)
```http
GET /api/weatherforecast/admin
Authorization: Bearer {access_token}
```

## Testing with Swagger

1. Navigate to `/swagger`
2. Click "Authorize" button
3. Enter: `Bearer {your_access_token}`
4. Click "Authorize"
5. Test protected endpoints

## Testing with cURL

### Register
```bash
curl -X POST https://localhost:7xxx/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "confirmPassword": "Test123!",
    "userName": "testuser"
  }'
```

### Login
```bash
curl -X POST https://localhost:7xxx/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### Access Protected Endpoint
```bash
curl -X GET https://localhost:7xxx/api/auth/me \
  -H "Authorization: Bearer {access_token}"
```

## Project Structure

```
SecureApi/
├── Configuration/
│   └── JwtSettings.cs              # JWT configuration model
├── Controllers/
│   ├── AuthController.cs           # Authentication endpoints
│   └── WeatherForecastController.cs # Sample protected endpoints
├── Data/
│   └── ApplicationDbContext.cs     # EF Core DbContext
├── Models/
│   ├── ApplicationUser.cs          # Extended IdentityUser
│   └── DTOs/
│       └── AuthDtos.cs             # Data transfer objects
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   └── ITokenService.cs
│   ├── AuthService.cs              # Authentication logic
│   └── TokenService.cs             # JWT token generation
├── appsettings.json
├── appsettings.Development.json
├── Program.cs                      # Application configuration
└── SecureApi.csproj
```

## Security Best Practices

### ✅ Implemented

1. **Strong Password Requirements**
   - Minimum 8 characters
   - Requires uppercase, lowercase, digit, and special character

2. **JWT Security**
   - Access tokens with short expiration (60 minutes)
   - Refresh tokens with longer expiration (7 days)
   - Secure token validation

3. **Account Lockout**
   - Locks account after 5 failed attempts
   - 5-minute lockout duration

4. **Secure Token Storage**
   - Refresh tokens stored in database
   - Token revocation support

### 🔒 Production Recommendations

1. **Environment Variables**
   - Store `SecretKey` in environment variables or Azure Key Vault
   - Never commit secrets to source control

2. **HTTPS**
   - Enable `RequireHttpsMetadata = true` in production
   - Use HTTPS redirection

3. **Database**
   - Use connection string from secure configuration
   - Enable SQL Server encryption

4. **CORS**
   - Restrict to specific origins instead of `AllowAll`
   - Example:
   ```csharp
   options.AddPolicy("Production", policy =>
   {
       policy.WithOrigins("https://yourdomain.com")
             .AllowAnyMethod()
             .AllowAnyHeader();
   });
   ```

5. **Rate Limiting**
   - Implement rate limiting middleware
   - Prevent brute force attacks

6. **Logging**
   - Log authentication attempts
   - Monitor suspicious activities

## Roles

Default roles are seeded on startup:
- `Admin` - Full access
- `Manager` - Management access
- `User` - Basic access

## Adding Custom Roles to Users

```csharp
// In your service or controller
await _userManager.AddToRoleAsync(user, "Admin");
```

## Common Issues

### Migration Error
```bash
# Remove migrations
dotnet ef migrations remove

# Create new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### Connection String Issues
- Verify SQL Server is running
- Check connection string format
- Ensure database name is valid

### JWT Token Issues
- Ensure SecretKey is at least 32 characters
- Verify token hasn't expired
- Check Bearer token format: `Bearer {token}`

## License

This template is provided as-is for educational and commercial use.

## Support

For issues and questions:
- Check Swagger documentation at `/swagger`
- Review logs in console output
- Verify database connection and migrations

---

**Made with ❤️ using ASP.NET Core 8**
