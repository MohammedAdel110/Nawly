namespace Nawly.Shared.EnumHelper.OrderEnums;

public enum CancelReasonCode
{
    CustomerChangedMind = 0,
    StoreTimeout = 1,
    StoreRejected = 2,
    ItemsUnavailable = 3,
    StoreTooBusy = 4,
    AddressOutOfRange = 5,
    CustomerUnreachable = 6,
    CourierFault = 7,
    PaymentTimeout = 8,
    DuplicateOrder = 9,
    FraudSuspected = 10,
    AdminIntervention = 11,
    Other = 12
}
