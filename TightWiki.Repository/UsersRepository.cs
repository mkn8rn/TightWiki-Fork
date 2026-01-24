using DAL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using System.Diagnostics.CodeAnalysis;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using static TightWiki.Library.Constants;
using DalProfile = DAL.Models.Profile;
using DalRole = DAL.Models.Role;
using DalAccountRole = DAL.Models.AccountRole;
using DalAccountPermissionEntity = DAL.Models.AccountPermissionEntity;
using DalRolePermissionEntity = DAL.Models.RolePermissionEntity;
using DalPermission = DAL.Models.Permission;
using DalPermissionDisposition = DAL.Models.PermissionDisposition;
using ApiAccountProfile = TightWiki.Models.DataModels.AccountProfile;
using ApiRole = TightWiki.Models.DataModels.Role;
using ApiPermission = TightWiki.Models.DataModels.Permission;
using ApiPermissionDisposition = TightWiki.Models.DataModels.PermissionDisposition;
using ApiRolePermission = TightWiki.Models.DataModels.RolePermission;
using ApiAccountPermission = TightWiki.Models.DataModels.AccountPermission;
using ApiApparentPermission = TightWiki.Models.DataModels.ApparentPermission;
using ApiProfileAvatar = TightWiki.Models.DataModels.ProfileAvatar;
using ApiAccountRoleMembership = TightWiki.Models.DataModels.AccountRoleMembership;
using ApiAddRoleMemberResult = TightWiki.Models.DataModels.AddRoleMemberResult;
using ApiAddAccountMembershipResult = TightWiki.Models.DataModels.AddAccountMembershipResult;
using ApiInsertRolePermissionResult = TightWiki.Models.DataModels.InsertRolePermissionResult;
using ApiInsertAccountPermissionResult = TightWiki.Models.DataModels.InsertAccountPermissionResult;

namespace TightWiki.Repository
{
    public interface IUsersRepository
    {
        bool IsAccountAMemberOfRole(Guid userId, int roleId);
        void DeleteRole(int roleId);
        bool InsertRole(string name, string? description);
        bool DoesRoleExist(string name);
        bool IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId);
        ApiInsertAccountPermissionResult? InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId);
        bool IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId);
        IEnumerable<ApiRole> AutoCompleteRole(string? searchText);
        IEnumerable<ApiAccountProfile> AutoCompleteAccount(string? searchText);
        ApiAddRoleMemberResult? AddRoleMemberByName(Guid userId, string roleName);
        ApiAddRoleMemberResult? AddRoleMember(Guid userId, int roleId);
        ApiAddAccountMembershipResult? AddAccountMembership(Guid userId, int roleId);
        void RemoveRoleMember(int roleId, Guid userId);
        void RemoveRolePermission(int id);
        void RemoveAccountPermission(int id);
        ApiInsertRolePermissionResult? InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId);
        List<ApiApparentPermission> GetApparentAccountPermissions(Guid userId);
        List<ApiApparentPermission> GetApparentRolePermissions(string roleName);
        List<ApiPermissionDisposition> GetAllPermissionDispositions();
        List<ApiPermission> GetAllPermissions();
        List<ApiRolePermission> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<ApiAccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null);
        void AnonymizeProfile(Guid userId);
        bool IsUserMemberOfAdministrators(Guid userId);
        ApiRole GetRoleByName(string name);
        List<ApiRole> GetAllRoles(string? orderBy = null, string? orderByDirection = null);
        List<ApiAccountProfile> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<ApiAccountPermission> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<ApiAccountRoleMembership> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<ApiAccountProfile> GetAllUsers();
        List<ApiAccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null);
        void CreateProfile(Guid userId, string accountName);
        bool DoesEmailAddressExist(string? emailAddress);
        bool DoesProfileAccountExist(string navigation);
        ApiAccountProfile? GetBasicProfileByUserId(Guid userId);
        ApiAccountProfile GetAccountProfileByUserId(Guid userId);
        void SetProfileUserId(string navigation, Guid userId);
        Guid? GetUserAccountIdByNavigation(string navigation);
        ApiAccountProfile GetAccountProfileByNavigation(string? navigation);
        ApiAccountProfile? GetAccountProfileByNavigationOrDefault(string? navigation);
        ApiProfileAvatar? GetProfileAvatarByNavigation(string navigation);
        void UpdateProfile(ApiAccountProfile item);
        void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType);
        AdminPasswordChangeState AdminPasswordStatus();
        void SetAdminPasswordClear();
        void SetAdminPasswordIsChanged();
        void SetAdminPasswordIsDefault();
    }

    public static class UsersRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IUsersRepository Repo =>
            _serviceProvider?.GetService(typeof(IUsersRepository)) as IUsersRepository
            ?? throw new InvalidOperationException("IUsersRepository is not configured.");

        public static bool IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId, roleId]);
            return WikiCache.AddOrGet(cacheKey, forceReCache, () => Repo.IsAccountAMemberOfRole(userId, roleId));
        }

        public static void DeleteRole(int roleId) => Repo.DeleteRole(roleId);

        public static bool InsertRole(string name, string? description) => Repo.InsertRole(name, description);

        public static bool DoesRoleExist(string name) => Repo.DoesRoleExist(name);

        public static bool IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId, permissionId, permissionDispositionId, ns, pageId]);
            return WikiCache.AddOrGet(cacheKey, forceReCache, () => Repo.IsAccountPermissionDefined(userId, permissionId, permissionDispositionId, ns, pageId));
        }

        public static InsertAccountPermissionResult? InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
            => Repo.InsertAccountPermission(userId, permissionId, permissionDisposition, ns, pageId);

        public static bool IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [roleId, permissionId, permissionDispositionId, ns, pageId]);
            return WikiCache.AddOrGet(cacheKey, forceReCache, () => Repo.IsRolePermissionDefined(roleId, permissionId, permissionDispositionId, ns, pageId));
        }

        public static IEnumerable<Role> AutoCompleteRole(string? searchText) => Repo.AutoCompleteRole(searchText);

        public static IEnumerable<AccountProfile> AutoCompleteAccount(string? searchText) => Repo.AutoCompleteAccount(searchText);

        public static AddRoleMemberResult? AddRoleMemberByname(Guid userId, string roleName) => Repo.AddRoleMemberByName(userId, roleName);

        public static AddRoleMemberResult? AddRoleMember(Guid userId, int roleId) => Repo.AddRoleMember(userId, roleId);

        public static AddAccountMembershipResult? AddAccountMembership(Guid userId, int roleId) => Repo.AddAccountMembership(userId, roleId);

        public static void RemoveRoleMember(int roleId, Guid userId) => Repo.RemoveRoleMember(roleId, userId);

        public static void RemoveRolePermission(int id) => Repo.RemoveRolePermission(id);

        public static void RemoveAccountPermission(int id) => Repo.RemoveAccountPermission(id);

        public static InsertRolePermissionResult? InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
            => Repo.InsertRolePermission(roleId, permissionId, permissionDisposition, ns, pageId);

        public static List<ApparentPermission> GetApparentAccountPermissions(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetApparentAccountPermissions(userId)).EnsureNotNull();
        }

        public static List<ApparentPermission> GetApparentRolePermissions(WikiRoles role)
            => GetApparentRolePermissions(role.ToString());

        public static List<ApparentPermission> GetApparentRolePermissions(string roleName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [roleName]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetApparentRolePermissions(roleName)).EnsureNotNull();
        }

        public static List<PermissionDisposition> GetAllPermissionDispositions()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetAllPermissionDispositions()).EnsureNotNull();
        }

        public static List<Permission> GetAllPermissions()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetAllPermissions()).EnsureNotNull();
        }

        public static List<RolePermission> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetRolePermissionsPaged(roleId, pageNumber, orderBy, orderByDirection, pageSize);

        public static List<AccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
            => Repo.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

        public static void AnonymizeProfile(Guid userId) => Repo.AnonymizeProfile(userId);

        public static bool IsUserMemberOfAdministrators(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.IsUserMemberOfAdministrators(userId));
        }

        public static Role GetRoleByName(string name) => Repo.GetRoleByName(name);

        public static List<Role> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
            => Repo.GetAllRoles(orderBy, orderByDirection);

        public static List<AccountProfile> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetRoleMembersPaged(roleId, pageNumber, orderBy, orderByDirection, pageSize);

        public static List<AccountPermission> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetAccountPermissionsPaged(userId, pageNumber, orderBy, orderByDirection, pageSize);

        public static List<AccountRoleMembership> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetAccountRoleMembershipPaged(userId, pageNumber, orderBy, orderByDirection, pageSize);

        public static List<AccountProfile> GetAllUsers() => Repo.GetAllUsers();

        public static List<AccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
            => Repo.GetAllUsersPaged(pageNumber, orderBy, orderByDirection, searchToken);

        public static void CreateProfile(Guid userId, string accountName) => Repo.CreateProfile(userId, accountName);

        public static bool DoesEmailAddressExist(string? emailAddress) => Repo.DoesEmailAddressExist(emailAddress);

        public static bool DoesProfileAccountExist(string navigation) => Repo.DoesProfileAccountExist(navigation);

        public static bool TryGetBasicProfileByUserId(Guid userId, [NotNullWhen(true)] out AccountProfile? accountProfile)
        {
            accountProfile = GetBasicProfileByUserId(userId);
            return accountProfile != null;
        }

        public static AccountProfile? GetBasicProfileByUserId(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetBasicProfileByUserId(userId));
        }

        public static AccountProfile GetAccountProfileByUserId(Guid userId, bool forceReCache = false)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
            return WikiCache.AddOrGet(cacheKey, forceReCache, () => Repo.GetAccountProfileByUserId(userId)).EnsureNotNull();
        }

        public static void SetProfileUserId(string navigation, Guid userId) => Repo.SetProfileUserId(navigation, userId);

        public static Guid? GetUserAccountIdByNavigation(string navigation) => Repo.GetUserAccountIdByNavigation(navigation);

        public static AccountProfile GetAccountProfileByNavigation(string? navigation) => Repo.GetAccountProfileByNavigation(navigation);

        public static bool TryGetAccountProfileByNavigation(string? navigation, [NotNullWhen(true)] out AccountProfile? accountProfile)
        {
            accountProfile = Repo.GetAccountProfileByNavigationOrDefault(navigation);
            return accountProfile != null;
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            // Note: This method is legacy and should not be needed with ASP.NET Identity
            throw new NotSupportedException("Use ASP.NET Identity for password verification.");
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            // Note: This method is legacy and should not be needed with ASP.NET Identity
            throw new NotSupportedException("Use ASP.NET Identity for password verification.");
        }

        public static ProfileAvatar? GetProfileAvatarByNavigation(string navigation) => Repo.GetProfileAvatarByNavigation(navigation);

        public static void UpdateProfile(AccountProfile item) => Repo.UpdateProfile(item);

        public static void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
            => Repo.UpdateProfileAvatar(userId, imageData, contentType);

        public static AdminPasswordChangeState AdminPasswordStatus() => Repo.AdminPasswordStatus();

        public static void SetAdminPasswordClear() => Repo.SetAdminPasswordClear();

        public static void SetAdminPasswordIsChanged() => Repo.SetAdminPasswordIsChanged();

        public static void SetAdminPasswordIsDefault() => Repo.SetAdminPasswordIsDefault();
    }

    public sealed class UsersRepositoryEf : IUsersRepository
    {
        private readonly WikiDbContext _wikiDb;
        private readonly IdentityDbContext _identityDb;

        public UsersRepositoryEf(WikiDbContext wikiDb, IdentityDbContext identityDb)
        {
            _wikiDb = wikiDb;
            _identityDb = identityDb;
        }

        public bool IsAccountAMemberOfRole(Guid userId, int roleId)
        {
            return _wikiDb.AccountRoles.AsNoTracking()
                .Any(ar => ar.UserId == userId && ar.RoleId == roleId);
        }

        public void DeleteRole(int roleId)
        {
            // Don't delete built-in roles
            var role = _wikiDb.Roles.SingleOrDefault(r => r.Id == roleId && !r.IsBuiltIn);
            if (role == null) return;

            // Delete related AccountRole entries
            var accountRoles = _wikiDb.AccountRoles.Where(ar => ar.RoleId == roleId);
            _wikiDb.AccountRoles.RemoveRange(accountRoles);

            // Delete related RolePermission entries
            var rolePermissions = _wikiDb.RolePermissions.Where(rp => rp.RoleId == roleId);
            _wikiDb.RolePermissions.RemoveRange(rolePermissions);

            // Delete the role
            _wikiDb.Roles.Remove(role);
            _wikiDb.SaveChanges();
        }

        public bool InsertRole(string name, string? description)
        {
            if (_wikiDb.Roles.Any(r => r.Name == name))
                return false;

            var role = new DalRole
            {
                Name = name,
                Description = description ?? string.Empty,
                IsBuiltIn = false
            };

            _wikiDb.Roles.Add(role);
            _wikiDb.SaveChanges();
            return true;
        }

        public bool DoesRoleExist(string name)
        {
            return _wikiDb.Roles.AsNoTracking().Any(r => r.Name == name);
        }

        public bool IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDispositionId)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            return _wikiDb.AccountPermissions.AsNoTracking()
                .Any(ap => ap.UserId == userId
                    && ap.PermissionId == permissionId
                    && ap.PermissionDispositionId == dispositionId
                    && ap.Namespace == ns
                    && ap.PageId == pageId);
        }

        public ApiInsertAccountPermissionResult? InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDisposition)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            var entity = new DalAccountPermissionEntity
            {
                UserId = userId,
                PermissionId = permissionId,
                PermissionDispositionId = dispositionId,
                Namespace = ns,
                PageId = pageId
            };

            _wikiDb.AccountPermissions.Add(entity);
            _wikiDb.SaveChanges();

            // Fetch the result with related data
            var permission = _wikiDb.Permissions.AsNoTracking().Single(p => p.Id == permissionId);
            var disposition = _wikiDb.PermissionDispositions.AsNoTracking().Single(pd => pd.Id == dispositionId);

            return new ApiInsertAccountPermissionResult
            {
                Id = entity.Id,
                Permission = permission.Name,
                PermissionDisposition = disposition.Name,
                Namespace = ns,
                PageId = pageId,
                ResourceName = ns ?? (pageId == "*" ? "*" : null) // Simplified resource name logic
            };
        }

        public bool IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDispositionId)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            return _wikiDb.RolePermissions.AsNoTracking()
                .Any(rp => rp.RoleId == roleId
                    && rp.PermissionId == permissionId
                    && rp.PermissionDispositionId == dispositionId
                    && rp.Namespace == ns
                    && rp.PageId == pageId);
        }

        public IEnumerable<ApiRole> AutoCompleteRole(string? searchText)
        {
            searchText ??= string.Empty;

            return _wikiDb.Roles.AsNoTracking()
                .Where(r => r.Name.Contains(searchText))
                .OrderBy(r => r.Name)
                .Take(25)
                .Select(r => new ApiRole
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsBuiltIn = r.IsBuiltIn
                })
                .ToList();
        }

        public IEnumerable<ApiAccountProfile> AutoCompleteAccount(string? searchText)
        {
            searchText ??= string.Empty;

            var profiles = _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.AccountName.Contains(searchText))
                .OrderBy(p => p.AccountName)
                .Take(25)
                .ToList();

            return profiles.Select(p => MapToAccountProfile(p)).ToList();
        }

        public ApiAddRoleMemberResult? AddRoleMemberByName(Guid userId, string roleName)
        {
            var role = _wikiDb.Roles.AsNoTracking().SingleOrDefault(r => r.Name == roleName);
            if (role == null) return null;

            return AddRoleMember(userId, role.Id);
        }

        public ApiAddRoleMemberResult? AddRoleMember(Guid userId, int roleId)
        {
            var accountRole = new DalAccountRole
            {
                UserId = userId,
                RoleId = roleId
            };

            _wikiDb.AccountRoles.Add(accountRole);
            _wikiDb.SaveChanges();

            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.UserId == userId);
            var user = _identityDb.Users.AsNoTracking().Single(u => u.Id == userId.ToString());

            return new ApiAddRoleMemberResult
            {
                Id = accountRole.Id,
                UserId = userId,
                Navigation = profile.Navigation,
                AccountName = profile.AccountName,
                EmailAddress = user.Email ?? string.Empty,
                FirstName = GetUserClaim(userId, "firstname"),
                LastName = GetUserClaim(userId, "lastname")
            };
        }

        public ApiAddAccountMembershipResult? AddAccountMembership(Guid userId, int roleId)
        {
            var accountRole = new DalAccountRole
            {
                UserId = userId,
                RoleId = roleId
            };

            _wikiDb.AccountRoles.Add(accountRole);
            _wikiDb.SaveChanges();

            var role = _wikiDb.Roles.AsNoTracking().Single(r => r.Id == roleId);

            return new ApiAddAccountMembershipResult
            {
                Id = accountRole.Id,
                Name = role.Name
            };
        }

        public void RemoveRoleMember(int roleId, Guid userId)
        {
            var entity = _wikiDb.AccountRoles.SingleOrDefault(ar => ar.RoleId == roleId && ar.UserId == userId);
            if (entity != null)
            {
                _wikiDb.AccountRoles.Remove(entity);
                _wikiDb.SaveChanges();
            }
        }

        public void RemoveRolePermission(int id)
        {
            var entity = _wikiDb.RolePermissions.SingleOrDefault(rp => rp.Id == id);
            if (entity != null)
            {
                _wikiDb.RolePermissions.Remove(entity);
                _wikiDb.SaveChanges();
            }
        }

        public void RemoveAccountPermission(int id)
        {
            var entity = _wikiDb.AccountPermissions.SingleOrDefault(ap => ap.Id == id);
            if (entity != null)
            {
                _wikiDb.AccountPermissions.Remove(entity);
                _wikiDb.SaveChanges();
            }
        }

        public ApiInsertRolePermissionResult? InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDisposition)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            var entity = new DalRolePermissionEntity
            {
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionDispositionId = dispositionId,
                Namespace = ns,
                PageId = pageId
            };

            _wikiDb.RolePermissions.Add(entity);
            _wikiDb.SaveChanges();

            var permission = _wikiDb.Permissions.AsNoTracking().Single(p => p.Id == permissionId);
            var disposition = _wikiDb.PermissionDispositions.AsNoTracking().Single(pd => pd.Id == dispositionId);

            return new ApiInsertRolePermissionResult
            {
                Id = entity.Id,
                Permission = permission.Name,
                PermissionDisposition = disposition.Name,
                Namespace = ns,
                PageId = pageId,
                ResourceName = ns ?? (pageId == "*" ? "*" : null)
            };
        }

        public List<ApiApparentPermission> GetApparentAccountPermissions(Guid userId)
        {
            // Permissions from roles
            var rolePermissions = (
                from rp in _wikiDb.RolePermissions.AsNoTracking()
                join ar in _wikiDb.AccountRoles.AsNoTracking() on rp.RoleId equals ar.RoleId
                join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                where ar.UserId == userId
                select new ApiApparentPermission
                {
                    Permission = p.Name,
                    PermissionDisposition = pd.Name,
                    Namespace = rp.Namespace,
                    PageId = rp.PageId
                }).ToList();

            // Direct account permissions
            var accountPermissions = (
                from ap in _wikiDb.AccountPermissions.AsNoTracking()
                join p in _wikiDb.Permissions.AsNoTracking() on ap.PermissionId equals p.Id
                join pd in _wikiDb.PermissionDispositions.AsNoTracking() on ap.PermissionDispositionId equals pd.Id
                where ap.UserId == userId
                select new ApiApparentPermission
                {
                    Permission = p.Name,
                    PermissionDisposition = pd.Name,
                    Namespace = ap.Namespace,
                    PageId = ap.PageId
                }).ToList();

            return rolePermissions.Concat(accountPermissions).ToList();
        }

        public List<ApiApparentPermission> GetApparentRolePermissions(string roleName)
        {
            return (
                from rp in _wikiDb.RolePermissions.AsNoTracking()
                join r in _wikiDb.Roles.AsNoTracking() on rp.RoleId equals r.Id
                join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                where r.Name == roleName
                select new ApiApparentPermission
                {
                    Permission = p.Name,
                    PermissionDisposition = pd.Name,
                    Namespace = rp.Namespace,
                    PageId = rp.PageId
                }).ToList();
        }

        public List<ApiPermissionDisposition> GetAllPermissionDispositions()
        {
            return _wikiDb.PermissionDispositions.AsNoTracking()
                .Select(pd => new ApiPermissionDisposition
                {
                    Id = pd.Id,
                    Name = pd.Name
                })
                .ToList();
        }

        public List<ApiPermission> GetAllPermissions()
        {
            return _wikiDb.Permissions.AsNoTracking()
                .Select(p => new ApiPermission
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToList();
        }

        public List<ApiRolePermission> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from rp in _wikiDb.RolePermissions.AsNoTracking()
                        join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                        join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                        where rp.RoleId == roleId
                        select new { rp, p, pd };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "permission" => isAsc ? query.OrderBy(x => x.p.Name) : query.OrderByDescending(x => x.p.Name),
                "permissiondisposition" => isAsc ? query.OrderBy(x => x.pd.Name) : query.OrderByDescending(x => x.pd.Name),
                _ => query.OrderBy(x => x.p.Name)
            };

            return query.Skip(skip).Take(pageSize.Value)
                .Select(x => new ApiRolePermission
                {
                    Id = x.rp.Id,
                    Permission = x.p.Name,
                    PermissionDisposition = x.pd.Name,
                    Namespace = x.rp.Namespace,
                    PageId = x.rp.PageId,
                    ResourceName = x.rp.Namespace ?? (x.rp.PageId == "*" ? "*" : null),
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<ApiAccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = _wikiDb.Profiles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchToken))
            {
                query = query.Where(p => p.AccountName.Contains(searchToken));
            }

            var profiles = query
                .OrderBy(p => p.AccountName)
                .Skip(skip)
                .Take(pageSize.Value)
                .ToList();

            return profiles.Select(p => MapToAccountProfile(p)).ToList();
        }

        public void AnonymizeProfile(Guid userId)
        {
            var profile = _wikiDb.Profiles.SingleOrDefault(p => p.UserId == userId);
            if (profile == null) return;

            var anonymousName = "DeletedUser_" + Utility.SanitizeAccountName($"{DateTime.UtcNow}", [' ']).Replace("_", "");

            profile.AccountName = anonymousName;
            profile.Navigation = Navigation.Clean(anonymousName);
            profile.Biography = null;
            profile.Avatar = null;
            profile.ModifiedDate = DateTime.UtcNow;

            _wikiDb.SaveChanges();
        }

        public bool IsUserMemberOfAdministrators(Guid userId)
        {
            return (
                from ar in _wikiDb.AccountRoles.AsNoTracking()
                join r in _wikiDb.Roles.AsNoTracking() on ar.RoleId equals r.Id
                where ar.UserId == userId && r.Name == "Administrator"
                select ar
            ).Any();
        }

        public ApiRole GetRoleByName(string name)
        {
            var role = _wikiDb.Roles.AsNoTracking().Single(r => r.Name == name);
            return new ApiRole
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsBuiltIn = role.IsBuiltIn
            };
        }

        public List<ApiRole> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var query = _wikiDb.Roles.AsNoTracking().AsQueryable();

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => isAsc ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
                "description" => isAsc ? query.OrderBy(r => r.Description) : query.OrderByDescending(r => r.Description),
                _ => query.OrderBy(r => r.Name)
            };

            return query.Select(r => new ApiRole
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsBuiltIn = r.IsBuiltIn
            }).ToList();
        }

        public List<ApiAccountProfile> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from ar in _wikiDb.AccountRoles.AsNoTracking()
                        join p in _wikiDb.Profiles.AsNoTracking() on ar.UserId equals p.UserId
                        where ar.RoleId == roleId
                        select p;

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "accountname" => isAsc ? query.OrderBy(p => p.AccountName) : query.OrderByDescending(p => p.AccountName),
                _ => query.OrderBy(p => p.AccountName)
            };

            var profiles = query.Skip(skip).Take(pageSize.Value).ToList();

            return profiles.Select(p =>
            {
                var profile = MapToAccountProfile(p);
                profile.PaginationPageSize = pageSize.Value;
                profile.PaginationPageCount = pageCount;
                return profile;
            }).ToList();
        }

        public List<ApiAccountPermission> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from ap in _wikiDb.AccountPermissions.AsNoTracking()
                        join p in _wikiDb.Permissions.AsNoTracking() on ap.PermissionId equals p.Id
                        join pd in _wikiDb.PermissionDispositions.AsNoTracking() on ap.PermissionDispositionId equals pd.Id
                        where ap.UserId == userId
                        select new { ap, p, pd };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "permission" => isAsc ? query.OrderBy(x => x.p.Name) : query.OrderByDescending(x => x.p.Name),
                _ => query.OrderBy(x => x.p.Name)
            };

            return query.Skip(skip).Take(pageSize.Value)
                .Select(x => new ApiAccountPermission
                {
                    Id = x.ap.Id,
                    Permission = x.p.Name,
                    PermissionDisposition = x.pd.Name,
                    Namespace = x.ap.Namespace,
                    PageId = x.ap.PageId,
                    ResourceName = x.ap.Namespace ?? (x.ap.PageId == "*" ? "*" : null),
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<ApiAccountRoleMembership> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from ar in _wikiDb.AccountRoles.AsNoTracking()
                        join r in _wikiDb.Roles.AsNoTracking() on ar.RoleId equals r.Id
                        where ar.UserId == userId
                        select new { ar, r };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => isAsc ? query.OrderBy(x => x.r.Name) : query.OrderByDescending(x => x.r.Name),
                _ => query.OrderBy(x => x.r.Name)
            };

            return query.Skip(skip).Take(pageSize.Value)
                .Select(x => new ApiAccountRoleMembership
                {
                    Id = x.ar.Id,
                    Name = x.r.Name,
                    RoleId = x.r.Id,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<ApiAccountProfile> GetAllUsers()
        {
            var profiles = _wikiDb.Profiles.AsNoTracking().ToList();
            return profiles.Select(p => MapToAccountProfile(p)).ToList();
        }

        public List<ApiAccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = _wikiDb.Profiles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchToken))
            {
                query = query.Where(p => p.AccountName.Contains(searchToken) || p.Navigation.Contains(searchToken));
            }

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "accountname" => isAsc ? query.OrderBy(p => p.AccountName) : query.OrderByDescending(p => p.AccountName),
                _ => query.OrderBy(p => p.AccountName)
            };

            var profiles = query.Skip(skip).Take(pageSize).ToList();

            return profiles.Select(p =>
            {
                var profile = MapToAccountProfile(p);
                profile.PaginationPageSize = pageSize;
                profile.PaginationPageCount = pageCount;
                return profile;
            }).ToList();
        }

        public void CreateProfile(Guid userId, string accountName)
        {
            var navigation = Navigation.Clean(accountName);
            if (_wikiDb.Profiles.Any(p => p.Navigation.ToLower() == navigation.ToLower()))
            {
                throw new Exception("An account with that name already exists");
            }

            var profile = new DalProfile
            {
                UserId = userId,
                AccountName = accountName,
                Navigation = navigation,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _wikiDb.Profiles.Add(profile);
            _wikiDb.SaveChanges();
        }

        public bool DoesEmailAddressExist(string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress)) return false;

            return _identityDb.Users.AsNoTracking()
                .Any(u => u.NormalizedEmail == emailAddress.ToUpperInvariant());
        }

        public bool DoesProfileAccountExist(string navigation)
        {
            if (string.IsNullOrWhiteSpace(navigation)) return false;

            return _wikiDb.Profiles.AsNoTracking()
                .Any(p => p.Navigation.ToLower() == navigation.ToLower());
        }

        public ApiAccountProfile? GetBasicProfileByUserId(Guid userId)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().SingleOrDefault(p => p.UserId == userId);
            return profile == null ? null : MapToAccountProfile(profile);
        }

        public ApiAccountProfile GetAccountProfileByUserId(Guid userId)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.UserId == userId);
            return MapToAccountProfile(profile);
        }

        public void SetProfileUserId(string navigation, Guid userId)
        {
            var profile = _wikiDb.Profiles.Single(p => p.Navigation == navigation);
            profile.UserId = userId;
            _wikiDb.SaveChanges();
        }

        public Guid? GetUserAccountIdByNavigation(string navigation)
        {
            return _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.Navigation == navigation)
                .Select(p => (Guid?)p.UserId)
                .FirstOrDefault();
        }

        public ApiAccountProfile GetAccountProfileByNavigation(string? navigation)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.Navigation == navigation);
            return MapToAccountProfile(profile);
        }

        public ApiAccountProfile? GetAccountProfileByNavigationOrDefault(string? navigation)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
            return profile == null ? null : MapToAccountProfile(profile);
        }

        public ApiProfileAvatar? GetProfileAvatarByNavigation(string navigation)
        {
            var profile = _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.Navigation == navigation)
                .Select(p => new { p.Avatar, p.AvatarContentType })
                .SingleOrDefault();

            if (profile?.Avatar == null) return null;

            return new ApiProfileAvatar
            {
                Bytes = profile.Avatar,
                ContentType = profile.AvatarContentType ?? "image/png"
            };
        }

        public void UpdateProfile(ApiAccountProfile item)
        {
            var profile = _wikiDb.Profiles.Single(p => p.UserId == item.UserId);
            profile.AccountName = item.AccountName;
            profile.Navigation = item.Navigation;
            profile.Biography = item.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            _wikiDb.SaveChanges();
        }

        public void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            var profile = _wikiDb.Profiles.Single(p => p.UserId == userId);
            profile.Avatar = imageData;
            profile.AvatarContentType = contentType;
            _wikiDb.SaveChanges();
        }

        public AdminPasswordChangeState AdminPasswordStatus()
        {
            var entry = _wikiDb.ConfigurationEntries.AsNoTracking()
                .SingleOrDefault(e => e.Name == "AdminPasswordChanged");

            if (entry == null)
            {
                return AdminPasswordChangeState.NeedsToBeSet;
            }

            if (bool.TryParse(entry.Value, out var isChanged) && isChanged)
            {
                return AdminPasswordChangeState.HasBeenChanged;
            }

            return AdminPasswordChangeState.IsDefault;
        }

        public void SetAdminPasswordClear()
        {
            var entry = _wikiDb.ConfigurationEntries.SingleOrDefault(e => e.Name == "AdminPasswordChanged");
            if (entry != null)
            {
                _wikiDb.ConfigurationEntries.Remove(entry);
                _wikiDb.SaveChanges();
            }
        }

        public void SetAdminPasswordIsChanged()
        {
            SetAdminPasswordConfigEntry("true");
        }

        public void SetAdminPasswordIsDefault()
        {
            SetAdminPasswordConfigEntry("false");
        }

        private void SetAdminPasswordConfigEntry(string value)
        {
            var entry = _wikiDb.ConfigurationEntries.SingleOrDefault(e => e.Name == "AdminPasswordChanged");
            if (entry == null)
            {
                entry = new DAL.Models.ConfigurationEntry
                {
                    Name = "AdminPasswordChanged",
                    Value = value,
                    ConfigurationGroupId = 1, // Basic configuration group
                    DataType = "string",
                    Description = "Indicates if admin password has been changed from default"
                };
                _wikiDb.ConfigurationEntries.Add(entry);
            }
            else
            {
                entry.Value = value;
            }
            _wikiDb.SaveChanges();
        }

        private ApiAccountProfile MapToAccountProfile(DalProfile profile)
        {
            var user = _identityDb.Users.AsNoTracking().SingleOrDefault(u => u.Id == profile.UserId.ToString());

            return new ApiAccountProfile
            {
                UserId = profile.UserId,
                AccountName = profile.AccountName,
                Navigation = profile.Navigation,
                Biography = profile.Biography,
                Avatar = profile.Avatar,
                CreatedDate = profile.CreatedDate,
                ModifiedDate = profile.ModifiedDate,
                EmailAddress = user?.Email ?? string.Empty,
                EmailConfirmed = user?.EmailConfirmed ?? false,
                FirstName = GetUserClaim(profile.UserId, "firstname"),
                LastName = GetUserClaim(profile.UserId, "lastname"),
                TimeZone = GetUserClaim(profile.UserId, "timezone") ?? string.Empty,
                Language = GetUserClaim(profile.UserId, "language") ?? string.Empty,
                Country = GetUserClaimEndingWith(profile.UserId, "/country") ?? string.Empty,
                Theme = GetUserClaim(profile.UserId, "theme")
            };
        }

        private string? GetUserClaim(Guid userId, string claimType)
        {
            return _identityDb.UserClaims.AsNoTracking()
                .Where(c => c.UserId == userId.ToString() && c.ClaimType == claimType)
                .Select(c => c.ClaimValue)
                .FirstOrDefault();
        }

        private string? GetUserClaimEndingWith(Guid userId, string claimTypeSuffix)
        {
            return _identityDb.UserClaims.AsNoTracking()
                .Where(c => c.UserId == userId.ToString() && c.ClaimType != null && c.ClaimType.EndsWith(claimTypeSuffix))
                .Select(c => c.ClaimValue)
                .FirstOrDefault();
        }
    }
}


