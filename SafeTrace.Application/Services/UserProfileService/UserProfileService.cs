using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices.common;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
namespace SafeTrace.Application.Services.UserProfileServices
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IFileStorageService _Image;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageUrlService _imageUrl;

        public UserProfileService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<UserProfileService> logger,
            IFileStorageService Image,
            IHttpContextAccessor httpContextAccessor,
            IUserService User,
            IUnitOfWork unitOfWork,
            IImageUrlService imageUrl
            )
        {

            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _Image = Image;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _imageUrl = imageUrl;
        }
        #region Profile Info
        public async Task<ApiResponse<GetUserInfoDTO?>> GetProfileInfoAsync(string userId)
        {
            _logger.LogInformation("Fetching profile for UserId: {userId} at {Time}", userId, DateTime.UtcNow);
            var user = await _userManager.Users
                .Include(u => u.Cases)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User With Id : {UserId} Not Found at {Time}", userId, DateTime.UtcNow);
                throw new NotFoundException($"المستخدم غير موجود");
            }
            else
            {
                var roles = await _userManager.GetRolesAsync(user);
                var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
                var dto = _mapper.Map<GetUserInfoDTO>(user);
                dto.Role = roles.FirstOrDefault() ?? UserRole.User.ToString();
                var request = _httpContextAccessor.HttpContext.Request;

                //string baseUrl = $"{request.Scheme}://{request.Host}";
                dto.ProfileImage = _imageUrl.Build(dto.ProfileImage);
                dto.IdentificationImageFront = _imageUrl.Build(dto.IdentificationImageFront);
                dto.IdentificationImageBack = _imageUrl.Build(dto.IdentificationImageBack);
                //dto.ProfileImage = string.IsNullOrEmpty(dto.ProfileImage)
                //    ? null
                //    : $"{baseUrl}{dto.ProfileImage}";

                //dto.IdentificationImageFront = string.IsNullOrEmpty(dto.IdentificationImageFront)
                //    ? null
                //    : $"{baseUrl}{dto.IdentificationImageFront}";
                //dto.IdentificationImageBack = string.IsNullOrEmpty(dto.IdentificationImageBack)
                //    ? null
                //    : $"{baseUrl}{dto.IdentificationImageBack}";
                return ApiResponse<GetUserInfoDTO?>.Ok(dto, ".اليك بيانات المستخدم");
            }
        }

        public async Task<ApiResponse<VisitUserDTO?>> GetVisitedUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) throw new NotFoundException($"المستخدم غير موجود");

            //var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            //var email = await _userManager.GetEmailAsync(user);
            var role = await _userManager.GetRolesAsync(user);

            var profile = _mapper.Map<VisitUserDTO>(user);
            //profile.PhoneNumber = phoneNumber;
            //profile.Email = email;
            profile.Role = role.FirstOrDefault() ?? UserRole.User.ToString();

            return ApiResponse<VisitUserDTO?>.Ok(profile, "تم جلب الملف الشخصي");
        }

        #endregion

        #region Update

        private async Task<ApplicationUser> GetUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                throw new NotFoundException("المستخدم غير موجود");

            return user;
        }

        private async Task SaveUser(ApplicationUser user)
        {
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BadRequestException(
                    string.Join(",", result.Errors.Select(x => x.Description)));
        }
        public async Task<ApiResponse<bool>> AddIdImageAsync(string userId, AddIdImageDTO dto)
        {
            var user = await GetUser(userId);
            if (dto.IdentificationImageFront is not null && dto.IdentificationImageBack is not null)
            {
                if (user.VerificationStatus == VerificationStatus.Verified)
                {
                    throw new BadRequestException("صورة البطاقة موجودة بالفعل .");
                }
                
                var newIdImageFront =
                 await _Image.SaveFileAsync(dto.IdentificationImageFront, "Identification/Front");
                var newIdImageBack =
                 await _Image.SaveFileAsync(dto.IdentificationImageBack, "Identification/Back");

                if (!string.IsNullOrEmpty(user.IdentificationImageFront))
                {
                    _Image.DeleteFile(user.IdentificationImageFront);
                }
                if (!string.IsNullOrEmpty(user.IdentificationImageback))
                {
                    _Image.DeleteFile(user.IdentificationImageback);
                }

                user.IdentificationImageFront = newIdImageFront;
                user.IdentificationImageback = newIdImageBack;
                user.VerificationStatus = VerificationStatus.Pending;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return ApiResponse<bool>.Ok(true, "فشل اضافة صورة بطاقة او تم اضافتها من قبل ");
                }
            }

            return ApiResponse<bool>.Ok(true, "تم اضافة صورة البطاقة بنجاح");

        }

        public async Task<ApiResponse<bool>> UpdateHomeLocationAsync(string userId, UpdateHomeLocationDTO dto)
        {
            var user = await GetUser(userId);

            user.HomeLocation = new Point(dto.HomeLongitude.Value, dto.HomeLatitude.Value) { SRID = 4326 };

            await SaveUser(user);
            return ApiResponse<bool>.Ok(true, "تم تحديث عنوانك بنجاح");
        }

        public async Task<ApiResponse<bool>> UpdateNameAsync(string userId, UpdateNameDTO dto)
        {
            var user = await GetUser(userId);
            dto.FirstName = dto.FirstName.Trim();
            dto.LastName = dto.LastName.Trim();
            
            _mapper.Map(dto, user);
            var result = await _userManager.UpdateAsync(user);
            return ApiResponse<bool>.Ok(true, "تم تحديث الاسم بنجاح");
            //}
        }


        public async Task<ApiResponse<bool>> UpdateProfilImageesync(string userId, UpdateProfileImageDTO dto)
        {
            var user = await GetUser(userId);
            if (dto.ProfileImage is not null)
            {
                var NewImg = await _Image.SaveFileAsync(dto.ProfileImage, "ProfileImages");

                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    _Image.DeleteFile(user.ProfileImage);
                }
                user.ProfileImage = NewImg;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    string.Join(", ", result.Errors.Select(e => e.Description));
                    //throw new BadRequestException("حدث خطأ اثناء محاولة اضافة صورة");
                    return ApiResponse<bool>.Ok(false, "حدث خطأ اثناء محاولة اضافة صورة");
                }
            }
            return ApiResponse<bool>.Ok(true, "تمت تغيير صورة الملف الشخصي بنجاح بنجاح  ");

        }

        public async Task<ApiResponse<bool>> RemoveProfileImageAsync(string userId)
        {
            var user = await GetUser(userId);

            //if (user is null)
            //    throw new NotFoundException("المستخدم غير موجود.");

            if (string.IsNullOrWhiteSpace(user.ProfileImage))
                return ApiResponse<bool>.Ok(true, "لا توجد صورة شخصية لحذفها.");

            _Image.DeleteFile(user.ProfileImage);

            user.ProfileImage = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BadRequestException("حدث خطأ أثناء حذف الصورة الشخصية.");

            return ApiResponse<bool>.Ok(true, "تم حذف الصورة الشخصية بنجاح.");
        }

        public async Task<ApiResponse<bool>> UpdatePhoneNumberAsync(string userId, ChangePhoneNumberDTO dto)
        {
            var user = await GetUser(userId);
            //if (user == null)
            //{
            //    throw new NotFoundException("المستخدم غير موجود");
            //}

            var newPhoneNumber = _mapper.Map(dto, user);
            var result = await _userManager.UpdateAsync(newPhoneNumber);
            if (!result.Succeeded)
            {
                return ApiResponse<bool>.Fail("حدث خطأ اثناء تغيير رقم الهاتف");
            }
            return ApiResponse<bool>.Ok(true, "تم تغيير رقم الهاتف بنجاح");

        }



        public async Task<ApiResponse<bool>> UpdateCurrentLocation(string userId, UpdateCurrentLocationDTO dto)
        {
            var user = await GetUser(userId);

            user.CurrentLocation = new Point(dto.CurrentLocationLongitude.Value, dto.CurrentLocationLatitude.Value) { SRID = 4326 };

            await SaveUser(user);

            return ApiResponse<bool>.Ok(true, "تم تحديث عنوانك بنجاح");
        }
        #endregion

        #region My Cases
        /// <summary>
        /// Retrieves paginated cases created by the current user,
        /// excluding soft-deleted cases.
        /// </summary>
        public async Task<ApiResponse<PaginationResponseDto<MyCaseListItemDto>>> GetMyCasesAsync(string userId, MyCasesFilterDto filter)
        {
            var query = _unitOfWork.Repository<Case>()
                .Query(
                    tracked: false,
                    includes: [x => x.AgeCategory, x => x.CaseFiles, x => x.FoundPersonInfo])
                .Where(x =>
                    x.UserId == userId &&
                    x.Status != CaseStatus.Deleted);

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var name = filter.FullName.Trim();

                query = query.Where(x =>
                    (x.FName ?? "").Contains(name) ||
                    (x.SName ?? "").Contains(name) ||
                    (x.TName ?? "").Contains(name) ||
                    (x.LName ?? "").Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.CaseCode))
            {
                query = query.Where(x => EF.Functions.Like(x.CaseCode, $"%{filter.CaseCode}%"));
            }

            if (filter.CaseType.HasValue)
            {
                query = query.Where(x => x.CaseType == filter.CaseType.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();

            var entities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = _mapper.Map<List<MyCaseListItemDto>>(entities);

            var result = new PaginationResponseDto<MyCaseListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };

            return ApiResponse<PaginationResponseDto<MyCaseListItemDto>>.Ok(result, "تم استرجاع الحالات الخاصة بالمستخدم بنجاح.");
        }



        #endregion

    }

}
