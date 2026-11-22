namespace FoodHub.Domain.Enums;

public enum CodeVerificationResult
{
    Success = 0,
    Wrong = 1,
    Expired = 2,
    Locked = 3,
    AlreadyUsed = 4
}
