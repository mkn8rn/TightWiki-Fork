using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Utils.Caching;
using TightWiki.Utils;
using static TightWiki.Contracts.Constants;

namespace BLL.Services.Users
{
    /// <summary>
    /// Business logic service for user and profile operations.
    /// </summary>
    public sealed class UsersService : IUsersService
    {
        private readonly WikiDbContext _wikiDb;
        private readonly IdentityDbContext _identityDb;

        public UsersService(WikiDbContext wikiDb, IdentityDbContext identityDb)
        {
            _wikiDb = wikiDb;
            _identityDb = identityDb;
        }

        #region Admin Password Management

        public AdminPasswordChangeState GetAdminPasswordStatus()
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
                entry = new DAL.Models.ConfigurationEntryDB
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

        #endregion

        #region Profile Management

        public void CreateProfile(Guid userId, string accountName)
        {
            var navigation = Navigation.Clean(accountName);
            if (_wikiDb.Profiles.Any(p => p.Navigation.ToLower() == navigation.ToLower()))
            {
                throw new System.Exception("An account with that name already exists");
            }

            var profile = new ProfileDB
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

        public Guid? GetUserAccountIdByNavigation(string navigation)
        {
            var userId = _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.Navigation.ToLower() == navigation.ToLower())
                .Select(p => p.UserId)
                .FirstOrDefault();

            return userId == Guid.Empty ? null : userId;
        }

        public void SetProfileUserId(string navigation, Guid userId)
        {
            var profile = _wikiDb.Profiles.SingleOrDefault(p => p.Navigation.ToLower() == navigation.ToLower());
            if (profile != null)
            {
                profile.UserId = userId;
                _wikiDb.SaveChanges();
            }
        }

        public AccountProfile GetAccountProfileByUserId(Guid userId)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.UserId == userId);
            return MapToAccountProfile(profile);
        }

        public AccountProfile? GetBasicProfileByUserId(Guid userId)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().SingleOrDefault(p => p.UserId == userId);
            return profile == null ? null : MapToAccountProfile(profile);
        }

        public bool TryGetBasicProfileByUserId(Guid userId, out AccountProfile? profile)
        {
            profile = GetBasicProfileByUserId(userId);
            return profile != null;
        }

        public AccountProfile GetAccountProfileByNavigation(string? navigation)
        {
            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.Navigation.ToLower() == (navigation ?? string.Empty).ToLower());
            return MapToAccountProfile(profile);
        }

        public bool DoesProfileAccountExist(string navigation)
        {
            if (string.IsNullOrWhiteSpace(navigation)) return false;
            return _wikiDb.Profiles.AsNoTracking()
                .Any(p => p.Navigation.ToLower() == navigation.ToLower());
        }

        public bool DoesEmailAddressExist(string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress)) return false;
            return _identityDb.Users.AsNoTracking()
                .Any(u => u.NormalizedEmail == emailAddress.ToUpperInvariant());
        }

        public void UpdateProfile(AccountProfile profile)
        {
            var entity = _wikiDb.Profiles.Single(p => p.UserId == profile.UserId);
            entity.AccountName = profile.AccountName;
            entity.Navigation = Navigation.Clean(profile.AccountName);
            entity.Biography = profile.Biography;
            entity.ModifiedDate = DateTime.UtcNow;
            _wikiDb.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.User);
        }

        public void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            var profile = _wikiDb.Profiles.Single(p => p.UserId == userId);
            profile.Avatar = imageData;
            profile.AvatarContentType = contentType;
            profile.ModifiedDate = DateTime.UtcNow;
            _wikiDb.SaveChanges();
        }

        public ProfileAvatar? GetProfileAvatarByNavigation(string navigation)
        {
            var profile = _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.Navigation.ToLower() == navigation.ToLower())
                .Select(p => new { p.Avatar, p.AvatarContentType })
                .SingleOrDefault();

            if (profile?.Avatar == null) return null;

            return new ProfileAvatar
            {
                Bytes = profile.Avatar,
                ContentType = profile.AvatarContentType ?? "image/png"
            };
        }

        public void AnonymizeProfile(Guid userId)
        {
            var profile = _wikiDb.Profiles.SingleOrDefault(p => p.UserId == userId);
            if (profile != null)
            {
                profile.AccountName = $"Anonymous_{Guid.NewGuid():N}";
                profile.Navigation = Navigation.Clean(profile.AccountName);
                profile.Biography = string.Empty;
                profile.Avatar = null;
                profile.AvatarContentType = null;
                profile.ModifiedDate = DateTime.UtcNow;
                _wikiDb.SaveChanges();
            }
        }

        public List<AccountProfile> GetAllUsers()
        {
            var profiles = _wikiDb.Profiles.AsNoTracking().ToList();
            return profiles.Select(MapToAccountProfile).ToList();
        }

        public List<AccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            IQueryable<ProfileDB> query = _wikiDb.Profiles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchToken))
            {
                query = query.Where(p => p.AccountName.Contains(searchToken) || p.Navigation.Contains(searchToken));
            }

            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "accountname" => asc ? query.OrderBy(p => p.AccountName) : query.OrderByDescending(p => p.AccountName),
                "createddate" => asc ? query.OrderBy(p => p.CreatedDate) : query.OrderByDescending(p => p.CreatedDate),
                "modifieddate" => asc ? query.OrderBy(p => p.ModifiedDate) : query.OrderByDescending(p => p.ModifiedDate),
                _ => query.OrderBy(p => p.AccountName)
            };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var profiles = query.Skip(skip).Take(pageSize).ToList();
            var result = profiles.Select(p =>
            {
                var profile = MapToAccountProfile(p);
                profile.PaginationPageSize = pageSize;
                profile.PaginationPageCount = pageCount;
                return profile;
            }).ToList();

            return result;
        }

        public List<AccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            IQueryable<ProfileDB> query = _wikiDb.Profiles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchToken))
            {
                query = query.Where(p => p.AccountName.Contains(searchToken));
            }

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var profiles = query.OrderBy(p => p.AccountName).Skip(skip).Take(pageSize.Value).ToList();
            var result = profiles.Select(p =>
            {
                var profile = MapToAccountProfile(p);
                profile.PaginationPageSize = pageSize.Value;
                profile.PaginationPageCount = pageCount;
                return profile;
            }).ToList();

            return result;
        }

        #endregion

        #region Role Management

        public AddRoleMemberResult? AddRoleMemberByName(Guid userId, string roleName)
        {
            var role = _wikiDb.Roles.AsNoTracking().SingleOrDefault(r => r.Name == roleName);
            if (role == null) return null;
            return AddRoleMember(userId, role.Id);
        }

        public AddRoleMemberResult? AddRoleMember(Guid userId, int roleId)
        {
            // Check if already a member
            if (_wikiDb.AccountRoles.Any(ar => ar.UserId == userId && ar.RoleId == roleId))
            {
                return null;
            }

            var accountRole = new AccountRoleDB
            {
                UserId = userId,
                RoleId = roleId
            };

            _wikiDb.AccountRoles.Add(accountRole);
            _wikiDb.SaveChanges();

            var profile = _wikiDb.Profiles.AsNoTracking().Single(p => p.UserId == userId);
            var user = _identityDb.Users.AsNoTracking().Single(u => u.Id == userId.ToString());

            return new AddRoleMemberResult
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

        public void RemoveRoleMember(int roleId, Guid userId)
        {
            var accountRole = _wikiDb.AccountRoles.SingleOrDefault(ar => ar.RoleId == roleId && ar.UserId == userId);
            if (accountRole != null)
            {
                _wikiDb.AccountRoles.Remove(accountRole);
                _wikiDb.SaveChanges();
            }
        }

        public List<Role> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            IQueryable<RoleDB> query = _wikiDb.Roles.AsNoTracking();

            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => asc ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
                _ => query.OrderBy(r => r.Name)
            };

            return query.Select(r => new Role
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsBuiltIn = r.IsBuiltIn
            }).ToList();
        }

        public Role GetRoleByName(string name)
        {
            var role = _wikiDb.Roles.AsNoTracking().Single(r => r.Name == name);
            return new Role
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsBuiltIn = role.IsBuiltIn
            };
        }

        public bool DoesRoleExist(string name)
        {
            return _wikiDb.Roles.AsNoTracking().Any(r => r.Name == name);
        }

        public bool InsertRole(string name, string? description)
        {
            if (_wikiDb.Roles.Any(r => r.Name == name))
                return false;

            var role = new RoleDB
            {
                Name = name,
                Description = description ?? string.Empty,
                IsBuiltIn = false
            };

            _wikiDb.Roles.Add(role);
            _wikiDb.SaveChanges();
            return true;
        }

        public void DeleteRole(int roleId)
        {
            var role = _wikiDb.Roles.SingleOrDefault(r => r.Id == roleId && !r.IsBuiltIn);
            if (role == null) return;

            var accountRoles = _wikiDb.AccountRoles.Where(ar => ar.RoleId == roleId);
            _wikiDb.AccountRoles.RemoveRange(accountRoles);

            var rolePermissions = _wikiDb.RolePermissions.Where(rp => rp.RoleId == roleId);
            _wikiDb.RolePermissions.RemoveRange(rolePermissions);

            _wikiDb.Roles.Remove(role);
            _wikiDb.SaveChanges();
        }

        public List<AccountProfile> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            var userIds = _wikiDb.AccountRoles.AsNoTracking()
                .Where(ar => ar.RoleId == roleId)
                .Select(ar => ar.UserId)
                .ToList();

            IQueryable<ProfileDB> query = _wikiDb.Profiles.AsNoTracking()
                .Where(p => userIds.Contains(p.UserId));

            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "accountname" => asc ? query.OrderBy(p => p.AccountName) : query.OrderByDescending(p => p.AccountName),
                _ => query.OrderBy(p => p.AccountName)
            };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var profiles = query.Skip(skip).Take(pageSize.Value).ToList();
            var result = profiles.Select(p =>
            {
                var profile = MapToAccountProfile(p);
                profile.PaginationPageSize = pageSize.Value;
                profile.PaginationPageCount = pageCount;
                return profile;
            }).ToList();

            return result;
        }

        public bool IsAccountAMemberOfRole(Guid userId, int roleId)
        {
            return _wikiDb.AccountRoles.AsNoTracking()
                .Any(ar => ar.UserId == userId && ar.RoleId == roleId);
        }

        public bool IsUserMemberOfAdministrators(Guid userId)
        {
            var adminRoleId = _wikiDb.Roles.AsNoTracking()
                .Where(r => r.Name == "Administrator")
                .Select(r => r.Id)
                .FirstOrDefault();

            return _wikiDb.AccountRoles.AsNoTracking()
                .Any(ar => ar.UserId == userId && ar.RoleId == adminRoleId);
        }

        public AddAccountMembershipResult? AddAccountMembership(Guid userId, int roleId)
        {
            var existingMembership = _wikiDb.AccountRoles.AsNoTracking()
                .Any(ar => ar.UserId == userId && ar.RoleId == roleId);

            if (existingMembership) return null;

            var accountRole = new AccountRoleDB
            {
                UserId = userId,
                RoleId = roleId
            };

            _wikiDb.AccountRoles.Add(accountRole);
            _wikiDb.SaveChanges();

            var role = _wikiDb.Roles.AsNoTracking().Single(r => r.Id == roleId);

            return new AddAccountMembershipResult
            {
                Id = accountRole.Id,
                Name = role.Name
            };
        }

        public List<AccountRoleMembership> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
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
                .Select(x => new AccountRoleMembership
                {
                    Id = x.ar.Id,
                    Name = x.r.Name,
                    RoleId = x.r.Id,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public IEnumerable<Role> AutoCompleteRole(string? searchText)
        {
            searchText ??= string.Empty;

            return _wikiDb.Roles.AsNoTracking()
                .Where(r => r.Name.Contains(searchText))
                .OrderBy(r => r.Name)
                .Take(25)
                .Select(r => new Role
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsBuiltIn = r.IsBuiltIn
                })
                .ToList();
        }

        public IEnumerable<AccountProfile> AutoCompleteAccount(string? searchText)
        {
            searchText ??= string.Empty;

            var profiles = _wikiDb.Profiles.AsNoTracking()
                .Where(p => p.AccountName.Contains(searchText))
                .OrderBy(p => p.AccountName)
                .Take(25)
                .ToList();

            return profiles.Select(MapToAccountProfile).ToList();
        }

        #endregion

        #region Permission Management

        public List<Permission> GetAllPermissions()
        {
            return _wikiDb.Permissions.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new Permission
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToList();
        }

        public List<PermissionDisposition> GetAllPermissionDispositions()
        {
            return _wikiDb.PermissionDispositions.AsNoTracking()
                .OrderBy(pd => pd.Name)
                .Select(pd => new PermissionDisposition
                {
                    Id = pd.Id,
                    Name = pd.Name
                })
                .ToList();
        }

        public List<ApparentPermission> GetApparentAccountPermissions(Guid userId)
        {
            // Get direct account permissions
            var accountPermissions = (from ap in _wikiDb.AccountPermissions.AsNoTracking()
                                      join p in _wikiDb.Permissions.AsNoTracking() on ap.PermissionId equals p.Id
                                      join pd in _wikiDb.PermissionDispositions.AsNoTracking() on ap.PermissionDispositionId equals pd.Id
                                      where ap.UserId == userId
                                      select new ApparentPermission
                                      {
                                          Permission = p.Name,
                                          PermissionDisposition = pd.Name,
                                          Namespace = ap.Namespace,
                                          PageId = ap.PageId
                                      }).ToList();

            // Get role-based permissions
            var roleIds = _wikiDb.AccountRoles.AsNoTracking()
                .Where(ar => ar.UserId == userId)
                .Select(ar => ar.RoleId)
                .ToList();

            var rolePermissions = (from rp in _wikiDb.RolePermissions.AsNoTracking()
                                   join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                                   join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                                   where roleIds.Contains(rp.RoleId)
                                   select new ApparentPermission
                                   {
                                       Permission = p.Name,
                                       PermissionDisposition = pd.Name,
                                       Namespace = rp.Namespace,
                                       PageId = rp.PageId
                                   }).ToList();

            return accountPermissions.Concat(rolePermissions).ToList();
        }

        public List<ApparentPermission> GetApparentRolePermissions(string roleName)
        {
            var roleId = _wikiDb.Roles.AsNoTracking()
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefault();

            return (from rp in _wikiDb.RolePermissions.AsNoTracking()
                    join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                    join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                    where rp.RoleId == roleId
                    select new ApparentPermission
                    {
                        Permission = p.Name,
                        PermissionDisposition = pd.Name,
                        Namespace = rp.Namespace,
                        PageId = rp.PageId
                    }).ToList();
        }

        public List<RolePermission> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from rp in _wikiDb.RolePermissions.AsNoTracking()
                        join p in _wikiDb.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                        join pd in _wikiDb.PermissionDispositions.AsNoTracking() on rp.PermissionDispositionId equals pd.Id
                        where rp.RoleId == roleId
                        select new RolePermission
                        {
                            Id = rp.Id,
                            Permission = p.Name,
                            PermissionDisposition = pd.Name,
                            Namespace = rp.Namespace,
                            PageId = rp.PageId
                        };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var result = query.Skip(skip).Take(pageSize.Value).ToList();

            foreach (var item in result)
            {
                item.PaginationPageSize = pageSize.Value;
                item.PaginationPageCount = pageCount;
            }

            return result;
        }

        public List<AccountPermission> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from ap in _wikiDb.AccountPermissions.AsNoTracking()
                        join p in _wikiDb.Permissions.AsNoTracking() on ap.PermissionId equals p.Id
                        join pd in _wikiDb.PermissionDispositions.AsNoTracking() on ap.PermissionDispositionId equals pd.Id
                        where ap.UserId == userId
                        select new AccountPermission
                        {
                            Id = ap.Id,
                            Permission = p.Name,
                            PermissionDisposition = pd.Name,
                            Namespace = ap.Namespace,
                            PageId = ap.PageId
                        };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var result = query.Skip(skip).Take(pageSize.Value).ToList();

            foreach (var item in result)
            {
                item.PaginationPageSize = pageSize.Value;
                item.PaginationPageCount = pageCount;
            }

            return result;
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

        public InsertAccountPermissionResult? InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDisposition)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            var entity = new DAL.Models.AccountPermissionEntityDB
            {
                UserId = userId,
                PermissionId = permissionId,
                PermissionDispositionId = dispositionId,
                Namespace = ns,
                PageId = pageId
            };

            _wikiDb.AccountPermissions.Add(entity);
            _wikiDb.SaveChanges();

            var permission = _wikiDb.Permissions.AsNoTracking().Single(p => p.Id == permissionId);
            var disposition = _wikiDb.PermissionDispositions.AsNoTracking().Single(pd => pd.Id == dispositionId);

            return new InsertAccountPermissionResult
            {
                Id = entity.Id,
                Permission = permission.Name,
                PermissionDisposition = disposition.Name,
                Namespace = ns,
                PageId = pageId,
                ResourceName = ns ?? (pageId == "*" ? "*" : null)
            };
        }

        public InsertRolePermissionResult? InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var dispositionId = _wikiDb.PermissionDispositions.AsNoTracking()
                .Where(pd => pd.Name == permissionDisposition)
                .Select(pd => pd.Id)
                .FirstOrDefault();

            var entity = new DAL.Models.RolePermissionEntityDB
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

            return new InsertRolePermissionResult
            {
                Id = entity.Id,
                Permission = permission.Name,
                PermissionDisposition = disposition.Name,
                Namespace = ns,
                PageId = pageId,
                ResourceName = ns ?? (pageId == "*" ? "*" : null)
            };
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

        public void RemoveRolePermission(int id)
        {
            var entity = _wikiDb.RolePermissions.SingleOrDefault(rp => rp.Id == id);
            if (entity != null)
            {
                _wikiDb.RolePermissions.Remove(entity);
                _wikiDb.SaveChanges();
            }
        }

        #endregion

        #region Private Helper Methods

        private AccountProfile MapToAccountProfile(ProfileDB profile)
        {
            var user = _identityDb.Users.AsNoTracking().SingleOrDefault(u => u.Id == profile.UserId.ToString());

            return new AccountProfile
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

        #endregion
    }
}

