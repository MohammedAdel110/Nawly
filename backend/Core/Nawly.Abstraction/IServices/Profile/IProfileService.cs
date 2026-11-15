using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Profile;

namespace Nawly.Abstraction.IServices.Profile;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken ct = default);
    Task<Result<UserDataExportDto>> ExportUserDataAsync(Guid userId, CancellationToken ct = default);
    Task<Result> RequestAccountDeletionAsync(Guid userId, CancellationToken ct = default);
}
