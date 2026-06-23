using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Models;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Auth;

public sealed class AuthService(
    FieldoreDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<ApiResponse<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "Email and password are required", 400);
        }

        var authUser = await dbContext.AuthUsers
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);

        if (authUser is null)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "Invalid email or password", 401);
        }

        var isValidPassword = passwordHasher.VerifyPassword(
            request.Password,
            authUser.PasswordHash,
            authUser.PasswordSalt);

        if (!isValidPassword)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "Invalid email or password", 401);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(x => x.AuthUserId == authUser.Id && x.IsActive, cancellationToken);

        if (profile is null)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "User profile not found", 404);
        }

        var businessId = await dbContext.Businesses
            .Where(x => x.AuthUserId == profile.AuthUserId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var token = tokenService.CreateToken(authUser, profile, businessId);

        return ApiResponse<AuthResponse>.Create(
            token, true, "Login successful", 200);
    }

    public async Task<ApiResponse<AuthResponse>> SignupAsync(
        SignupRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "All fields are required", 400);
        }

        var emailExists = await dbContext.AuthUsers
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (emailExists)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "Email already exists", 409);
        }

        var (authUser, profile) = CreateUserAggregate(
            email,
            request.Password,
            request.FirstName,
            request.LastName);

        dbContext.AuthUsers.Add(authUser);
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateToken(authUser, profile);

        return ApiResponse<AuthResponse>.Create(
            token, true, "Signup successful", 201);
    }

    // public async Task<ApiResponse<AuthResponse>> BusinessRegisterAsync(
    //     BusinessRegisterRequest request,
    //     CancellationToken cancellationToken = default)
    // {
    //     var email = NormalizeEmail(request.Email);
    //
    //     if (string.IsNullOrWhiteSpace(email) ||
    //         string.IsNullOrWhiteSpace(request.Password) ||
    //         string.IsNullOrWhiteSpace(request.FirstName) ||
    //         string.IsNullOrWhiteSpace(request.LastName))
    //     {
    //         return ApiResponse<AuthResponse>.Create(
    //             null, false, "Required fields are missing", 400);
    //     }
    //
    //     if (string.IsNullOrWhiteSpace(request.BusinessName))
    //     {
    //         return ApiResponse<AuthResponse>.Create(
    //             null, false, "Business name is required", 400);
    //     }
    //
    //     var emailExists = await dbContext.AuthUsers
    //         .AnyAsync(x => x.Email == email, cancellationToken);
    //
    //     if (emailExists)
    //     {
    //         return ApiResponse<AuthResponse>.Create(
    //             null, false, "Email already exists", 409);
    //     }
    //
    //     var (authUser, profile) = CreateUserAggregate(
    //         email,
    //         request.Password,
    //         request.FirstName,
    //         request.LastName);
    //
    //     var businessId = Guid.NewGuid();
    //
    //     var business = new Business
    //     {
    //         Id = businessId,
    //         Name = request.BusinessName.Trim(),
    //         TradeType = NormalizeOptional(request.TradeType),
    //         Email = NormalizeOptional(request.BusinessEmail) ?? email,
    //         Phone = NormalizeOptional(request.Phone)
    //     };
    //
    //     var membership = new BusinessMembership
    //     {
    //         Id = Guid.NewGuid(),
    //         BusinessId = businessId,
    //         UserProfileId = profile.Id,
    //         Role = BusinessMembershipRoles.Owner,
    //         IsPrimary = true,
    //         IsActive = true
    //     };
    //
    //     dbContext.AuthUsers.Add(authUser);
    //     dbContext.UserProfiles.Add(profile);
    //     dbContext.Businesses.Add(business);
    //     dbContext.BusinessMemberships.Add(membership);
    //
    //     await dbContext.SaveChangesAsync(cancellationToken);
    //
    //     var token = tokenService.CreateToken(authUser, profile, businessId);
    //
    //     return ApiResponse<AuthResponse>.Create(
    //         token, true, "Business registered successfully", 201);
    // }
    public async Task<ApiResponse<AuthResponse>> BusinessRegisterAsync(
        Guid userId,
        BusinessRegisterRequest request,
        CancellationToken cancellationToken)
    {
        // 🔍 Validate user
        var authUser = await dbContext.AuthUsers
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (authUser == null)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "User not found", 404);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(x => x.AuthUserId == userId, cancellationToken);

        if (profile == null)
        {
            return ApiResponse<AuthResponse>.Create(
                null, false, "User profile not found", 404);
        }

        // ❌ Prevent duplicate business (optional rule)
        var alreadyExists = await dbContext.Businesses
            .Where(x => x.AuthUserId == userId).FirstOrDefaultAsync(cancellationToken);

        if (alreadyExists! != null)
        {
            alreadyExists.Name = request.BusinessName;
            alreadyExists.TradeType = request.TradeType;
            alreadyExists.Phone = request.Phone;


            alreadyExists.Address.Line1 = request.AddressLine1;
            alreadyExists.Address.Line2 = request.AddressLine2;
            alreadyExists.Address.City = request.City;
            alreadyExists.Address.StateOrProvince = request.StateOrProvince;
            alreadyExists.Address.PostalCode = request.PostalCode;
            alreadyExists.Address.Country = request.Country;
            if (!string.IsNullOrWhiteSpace(request.Currency))
            {
                alreadyExists.Currency = NormalizeCurrencyCode(request.Currency);
            }
            alreadyExists.UpdatedAt = DateTimeOffset.UtcNow;
            if (string.IsNullOrWhiteSpace(profile.Phone))
            {
                profile.Phone = request.Phone;
            }

            if (string.IsNullOrWhiteSpace(profile.TimeZone))
            {
                profile.TimeZone = request.TimeZone;
            }

            profile.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            // 🔑 Generate new token with business_id
            var updateresponse = tokenService.CreateToken(authUser, profile);

            return ApiResponse<AuthResponse>.Create(
                updateresponse, true, "Business Update Sucessfully", 200);
        }

        // 🏗 Create business
        var business = new Business
        {
            Id = Guid.NewGuid(),
            AuthUserId = userId, // 🔥 from claims

            Name = request.BusinessName,
            TradeType = request.TradeType,
            Phone = request.Phone,
            Email = profile.Email, // optional mapping
            Currency = NormalizeCurrencyCode(request.Currency),

            Address = new Address()
            {
                Line1 = request.AddressLine1,
                Line2 = request.AddressLine2,
                City = request.City,
                StateOrProvince = request.StateOrProvince,
                PostalCode = request.PostalCode,
                Country = request.Country
            },

            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.Businesses.AddAsync(business, cancellationToken);

// 🧠 Update profile (safe update - only if empty)
        bool profileUpdated = false;

        if (string.IsNullOrWhiteSpace(profile.Phone))
        {
            profile.Phone = request.Phone;
            profileUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(profile.TimeZone))
        {
            profile.TimeZone = request.TimeZone;
            profileUpdated = true;
        }

        if (profileUpdated)
        {
            profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        // 🔑 Generate new token with business_id
        var response = tokenService.CreateToken(authUser, profile, business.Id);

        return ApiResponse<AuthResponse>.Create(
            response, true, "Business registered successfully", 200);
    }

    public async Task<ApiResponse<BusinessDetailsResponse>> GetBusinessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthUserId == userId, cancellationToken);

        if (business == null)
        {
            return ApiResponse<BusinessDetailsResponse>.Create(
                null, false, "Business not found", 404);
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthUserId == userId, cancellationToken);

        var response = new BusinessDetailsResponse
        {
            Id = business.Id,
            Name = business.Name,
            TradeType = business.TradeType,
            Phone = business.Phone,
            Email = business.Email,

            Address = new AddressDto
            {
                Line1 = business.Address?.Line1,
                Line2 = business.Address?.Line2,
                City = business.Address?.City,
                StateOrProvince = business.Address?.StateOrProvince,
                PostalCode = business.Address?.PostalCode,
                Country = business.Address?.Country
            },

            TimeZone = profile?.TimeZone,
            Currency = string.IsNullOrWhiteSpace(business.Currency) ? "USD" : business.Currency
        };

        return ApiResponse<BusinessDetailsResponse>.Create(
            response, true, "Business fetched successfully", 200);
    }

    public Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Task.FromResult(
                ApiResponse<ForgotPasswordResponse>.Create(
                    null, false, "Email is required", 400));
        }

        var response = new ForgotPasswordResponse(
            "If the email exists, a password reset process can be started.");

        return Task.FromResult(
            ApiResponse<ForgotPasswordResponse>.Create(
                response, true, "Request processed", 200));
    }

    private static string NormalizeCurrencyCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? "USD" : code.Trim().ToUpperInvariant();
    }

    private async Task EnsureEmailIsAvailableAsync(string email, CancellationToken cancellationToken)
    {
        var exists = await dbContext.AuthUsers.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }
    }

    private (AuthUser AuthUser, AppUserProfile Profile) CreateUserAggregate(string email, string password,
        string firstName, string lastName)
    {
        var authUserId = Guid.NewGuid();
        var (hash, salt) = passwordHasher.HashPassword(password);
        var trimmedFirstName = firstName.Trim();
        var trimmedLastName = lastName.Trim();

        var authUser = new AuthUser
        {
            Id = authUserId,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true
        };

        var profile = new AppUserProfile
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = email,
            FirstName = trimmedFirstName,
            LastName = trimmedLastName,
            DisplayName = $"{trimmedFirstName} {trimmedLastName}".Trim(),
            IsActive = true
        };

        return (authUser, profile);
    }

    private static void ValidateLoginRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email and password are required.");
        }
    }

    private static void ValidateSignupRequest(string email, string password, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException("Email, password, first name, and last name are required.");
        }

        if (password.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters long.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}