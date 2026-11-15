export type Language = 'Ar' | 'En';

export type UiLocale = 'ar' | 'en';

export type OtpPurpose = 'Login' | 'PhoneChange' | 'SensitiveAction';

export type DeliveryMode = 'PlatformCourier' | 'SelfDelivery' | 'PickupOnly';

export type VehicleType = 'Bicycle' | 'Motorcycle' | 'Scooter' | 'Car' | 'OnFoot';

export type StoreStatus = 'Pending' | 'Active' | 'Suspended' | 'Closed' | 'Rejected';

export type CourierStatus = 'Pending' | 'Approved' | 'Suspended' | 'Rejected';

export type UserStatus = 'Active' | 'Suspended' | 'Deleted' | 'PendingVerification';

export type RoleName =
  | 'Customer'
  | 'StoreOwner'
  | 'StoreStaff'
  | 'Courier'
  | 'AdminSuper'
  | 'AdminOperations'
  | 'AdminFinance'
  | 'AdminSupport';

export const ADMIN_ROLES: readonly RoleName[] = [
  'AdminSuper',
  'AdminOperations',
  'AdminFinance',
  'AdminSupport',
];

export const STORE_ROLES: readonly RoleName[] = ['StoreOwner', 'StoreStaff'];

export interface RequestOtpDto {
  phoneNumber: string;
  purpose: OtpPurpose;
  captchaToken?: string | null;
}

export interface OtpChallengeResponseDto {
  expiresAt: string;
  cooldownSeconds: number;
}

export interface VerifyOtpDto {
  phoneNumber: string;
  code: string;
  purpose: OtpPurpose;
  deviceName?: string | null;
}

export interface LoginRequestDto {
  identifier: string;
  password: string;
  deviceName?: string | null;
}

export interface MfaVerifyRequestDto {
  challengeToken: string;
  code: string;
  isRecoveryCode: boolean;
  deviceName?: string | null;
}

export interface RefreshTokenRequestDto {
  refreshToken?: string | null;
}

export interface LogoutRequestDto {
  refreshToken?: string | null;
}

export interface AuthTokenDto {
  accessToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  fullName: string;
  preferredLanguage: Language;
  roles: RoleName[];
  storeId?: string | null;
  refreshToken?: string | null;
  refreshTokenExpiresAt?: string | null;
}

export interface LoginResponseDto {
  requiresMfa: boolean;
  mfaChallengeToken: string | null;
  tokens: AuthTokenDto | null;
}

export interface ProfileDto {
  id: string;
  phoneNumber: string;
  fullName: string;
  email: string | null;
  preferredLanguage: Language;
  status: UserStatus;
  roles: RoleName[];
}

export interface UpdateProfileDto {
  fullName: string;
  email?: string | null;
  preferredLanguage: Language;
}

export interface StoreCategoryDto {
  id: string;
  name: string;
  nameAr: string;
  iconRef: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface DeliveryZoneDto {
  id: string;
  name: string;
  nameAr: string;
  baseFee: number;
  perKmFee: number;
  minOrderAmount: number;
  freeDeliveryThreshold: number | null;
  estimatedTravelMinutes: number;
  isActive: boolean;
}

export interface AreaDto {
  id: string;
  zoneId: string;
  zoneName: string;
  zoneNameAr: string;
  name: string;
  nameAr: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ApplyStoreDto {
  ownerFullName: string;
  ownerPhone: string;
  ownerPassword: string;
  storeName: string;
  storeNameAr: string;
  description?: string | null;
  descriptionAr?: string | null;
  categoryId: string;
  areaId: string;
  addressText: string;
  storePhone: string;
  latitude?: number | null;
  longitude?: number | null;
  deliveryMode: DeliveryMode;
  minOrderAmount: number;
  defaultPrepMinutes: number;
  deliveryZoneIds: string[];
}

export interface PaginationResponse<T> {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  data: T[];
}

export interface StoreSummaryDto {
  id: string;
  slug: string;
  name: string;
  nameAr: string;
  categoryName: string;
  categoryNameAr: string;
  logoRef: string | null;
  deliveryFee: number;
  minOrderAmount: number;
  estimatedMinutes: number;
  ratingAvg: number;
  reviewCount?: number;
  ratingCount?: number;
  isOpenNow: boolean;
  deliveryMode: DeliveryMode;
}

export interface StoreDetailsDto {
  id: string;
  name: string;
  nameAr: string;
  slug: string;
  description: string | null;
  descriptionAr: string | null;
  categoryId: string;
  areaId: string;
  addressText: string;
  phone: string;
  latitude: number | null;
  longitude: number | null;
  deliveryMode: DeliveryMode;
  status: StoreStatus;
  minOrderAmount: number;
  defaultPrepMinutes: number;
  logoRef: string | null;
  coverRef: string | null;
  averageRating: number;
  reviewCount: number;
  deliveryZoneIds: string[];
  rejectionReason?: string | null;
}

export interface ApplyCourierDto {
  fullName: string;
  phoneNumber: string;
  password: string;
  vehicleType: VehicleType;
  nationalIdImageRef: string;
  photoRef?: string | null;
}

export interface CourierProfileDto {
  id: string;
  userId: string;
  fullName: string;
  phoneNumber: string;
  vehicleType: VehicleType;
  status: CourierStatus;
  isAvailable: boolean;
  cashOnHandLimit: number;
  currentCashBalance: number;
  rejectionReason: string | null;
  approvedAt: string | null;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  retryAfterSeconds?: number;
  [key: string]: unknown;
}
