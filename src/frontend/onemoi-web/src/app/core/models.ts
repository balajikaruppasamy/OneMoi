// Shapes returned by the .NET API (keep in sync with OneMoi.Application DTOs)

export type UserType = 'SuperAdmin' | 'TenantUser' | 'Individual';

export interface SessionProfile {
  principalType: 'User' | 'Operator';
  id: number;
  name: string;
  nameTa?: string | null;
  userType?: UserType | null;
  role?: string | null;
  tenantId?: number | null;
  tenantCode?: string | null;
  tenantName?: string | null;
  tenantLogo?: string | null;
  tenantStatus?: string | null;
  personId?: number | null;
  mobile?: string | null;
  email?: string | null;
  isHost: boolean;
  home: string;
  hasPassword: boolean;
  /** What this login may do — see backend Application/Common/Security/Permissions.cs */
  permissions: string[];
}

export interface AuthResponse { accessToken: string; expiresAt: string; refreshToken: string; profile: SessionProfile; }
export interface SendOtpResponse { sentTo: string; channel: string; expiresInSeconds: number; resendAfterSeconds: number; devOtp?: string | null; }
export interface ApiError { message: string; code?: string; errors?: Record<string, string[]> | null; }

export interface MasterItem {
  id: number; name: string; nameTa?: string | null; sortOrder: number; isActive: boolean; isSystem: boolean;
  isHighlighted: boolean; color?: string | null; unit?: string | null;
}
export interface Denomination { id: number; value: number; isCoin: boolean; sortOrder: number; }

export interface FunctionListItem {
  id: number; code: string; name: string; nameTa?: string; typeName: string; typeNameTa?: string; ownerName: string; ownerNameTa?: string;
  ownerMobile: string; location: string; functionDate: string; startTime?: string; endTime?: string; status: string;
  counterCount: number; entryCount: number; collected: number;
}
export interface CounterDto { id: number; number: number; name: string; operatorId?: number; operatorName?: string; operatorCode?: string; }
export interface FunctionDetail {
  id: number; code: string; functionTypeId: number; typeName: string; typeNameTa?: string;
  name: string; nameTa?: string; ownerName: string; ownerNameTa?: string; ownerMobile: string; ownerEmail?: string;
  location: string; locationTa?: string; address?: string; city?: string; functionDate: string; startTime?: string; endTime?: string;
  expectedGuests?: number; otherDetails?: string; allowCash: boolean; allowUpi: boolean; status: string; counters: CounterDto[];
  tenantName: string; tenantNameTa?: string; tenantLogo?: string;
}

export interface DenominationLine { noteValue: number; count: number; }
export interface GiftLine { giftItemTypeId?: number | null; description: string; descriptionTa?: string | null; quantity: number; estimatedValue?: number | null; }
export interface MoiEntry {
  id: number; serialNo: number; receiptNo: string; entryAt: string; mobile?: string; initial?: string; name: string; nameTa?: string;
  spouseInitial?: string; spouseName?: string; spouseNameTa?: string; work?: string; city?: string; cityTa?: string;
  moiCategoryId?: number; categoryName?: string; categoryNameTa?: string; isHighlighted: boolean; highlightColor?: string;
  amount: number; paymentMode: string; paymentRef?: string; notes?: string; status: string; reversalReason?: string;
  counterName?: string; operatorName?: string; denominations: DenominationLine[]; gifts: GiftLine[];
}
export interface PersonLookup {
  source: 'tenant' | 'global' | 'none'; initial?: string; name?: string; nameTa?: string; spouseInitial?: string; spouseName?: string;
  spouseNameTa?: string; work?: string; city?: string; cityTa?: string; previousEntriesAtThisVendor: number; isVerified: boolean;
  sameNameInCity: { initial?: string; name: string; spouseName?: string; mobileMasked?: string }[];
}
export interface Expense {
  id: number; entryAt: string; categoryName?: string; categoryNameTa?: string; takenByName: string; takenByNameTa?: string; relation?: string;
  takenByMobile?: string; purpose: string; amount: number; paymentMode: string; notes?: string; recordedBy?: string;
}
export interface NameAmount { name: string; nameTa?: string; count: number; amount: number; }
export interface FunctionSummary {
  functionId: number; functionName: string; functionNameTa?: string; entries: number; reversedEntries: number; totalCollected: number;
  cash: number; upi: number; otherModes: number; giftCount: number; totalExpenses: number; cashExpenses: number; cashInHand: number;
  byCounter: NameAmount[]; byCategory: NameAmount[]; cashDenominations: DenominationLine[];
  highlighted: { category: string; categoryTa?: string; color?: string; name: string; nameTa?: string; amount: number }[];
}

export interface OperatorDto { id: number; code: string; name: string; nameTa?: string; mobile: string; isActive: boolean; lastLoginAt?: string; totalEntries: number; todayAssignment?: string; }
export interface OperatorCredentials { id: number; tenantCode: string; operatorCode: string; pin: string; }
export interface AssignmentBoard {
  date: string;
  functions: { id: number; name: string; nameTa?: string; typeName: string; location: string; startTime?: string; endTime?: string; status: string;
    counters: { id: number; number: number; name: string; assignmentId?: number; operatorId?: number; operatorName?: string; operatorCode?: string }[] }[];
  operators: { id: number; code: string; name: string; mobile: string; assignedFunctionId?: number }[];
}
export interface MyAssignment {
  assignmentId: number; functionId: number; functionName: string; functionNameTa?: string; location: string; functionDate: string;
  counterId: number; counterName: string; validFrom: string; validTo: string; isOpenNow: boolean; status: string;
}

export interface TenantProfile {
  id: number; code: string; name: string; nameTa?: string; tagline?: string; ownerName: string; email?: string; mobile: string; altMobile?: string;
  addressLine?: string; city?: string; district?: string; state: string; pincode?: string; gstin?: string; logoPath?: string; primaryColor?: string;
  receiptHeader?: string; receiptFooter?: string; status: string; planName?: string; trialEndsAt?: string;
}
export interface TenantMember { id: number; userId: number; name: string; mobile?: string; email?: string; role: string; isActive: boolean; lastLoginAt?: string; isLocked: boolean; isYou: boolean; }
export interface RolePermissions { role: string; permissions: string[]; }

export interface VendorDashboard {
  todayCollected: number; todayEntries: number; todayCash: number; todayUpi: number; todayExpenses: number; functionsToday: number;
  upcomingFunctions: number; activeOperators: number; monthCollected: number;
  today: { id: number; name: string; nameTa?: string; typeName: string; location: string; status: string; entries: number; collected: number; operators: string[] }[];
  recent: { id: number; entryAt: string; name: string; nameTa?: string; city?: string; amount: number; paymentMode: string; functionName: string; operatorName?: string; isHighlighted: boolean }[];
}

export interface MyMoiItem {
  entryId: number; entryAt: string; functionDate: string; functionName: string; functionNameTa?: string; functionType: string; functionTypeTa?: string;
  location: string; ownerName?: string; vendorName: string; vendorNameTa?: string; vendorLogo?: string; nameAsWritten?: string; nameTaAsWritten?: string;
  amount: number; paymentMode: string; category?: string; categoryTa?: string; isHighlighted: boolean; receiptNo: string; gifts: string[];
}
export interface MyMoiResponse { total: number; count: number; vendorCount: number; years: number[]; byVendor: { vendor: string; count: number; amount: number }[]; items: MyMoiItem[]; }
export interface MyProfile {
  userId: number; mobile?: string; email?: string; fullName: string; fullNameTa?: string; initial?: string; name?: string; nameTa?: string;
  spouseInitial?: string; spouseName?: string; spouseNameTa?: string; work?: string; city?: string; cityTa?: string;
}
export interface HostedFunction { id: number; code: string; name: string; nameTa?: string; typeName: string; functionDate: string; location: string; status: string; vendorName: string; vendorLogo?: string; entries: number; total: number; }

export interface FunctionReport {
  header: {
    vendorName: string; vendorNameTa?: string; vendorLogo?: string; vendorAddress?: string; vendorMobile?: string; receiptHeader?: string; receiptFooter?: string;
    functionName: string; functionNameTa?: string; functionType: string; functionTypeTa?: string; ownerName: string; ownerNameTa?: string;
    location: string; locationTa?: string; functionDate: string; code: string;
  };
  rows: {
    serialNo: number; receiptNo: string; entryAt: string; initial?: string; name: string; nameTa?: string; spouseInitial?: string; spouseName?: string;
    spouseNameTa?: string; work?: string; city?: string; cityTa?: string; mobile?: string; category?: string; categoryTa?: string; isHighlighted: boolean;
    amount: number; paymentMode: string; counter?: string; gifts: string;
  }[];
  total: number; cash: number; upi: number; expenses: number; netCash: number; count: number;
}

export interface AdminDashboard { activeTenants: number; pendingTenants: number; suspendedTenants: number; functionsThisMonth: number; persons: number; verifiedPersons: number; users: number; moiThisMonth: number; entriesThisMonth: number; }
export interface AdminTenant { id: number; code: string; name: string; city?: string; ownerName: string; mobile: string; email?: string; status: string; plan?: string; functions: number; operators: number; totalMoi: number; createdAt: string; logoPath?: string; }
export interface AdminUser { id: number; fullName: string; mobile?: string; email?: string; userType: string; tenant?: string; isActive: boolean; lastLoginAt?: string; createdAt: string; isLocked: boolean; }
export interface OtpLog { id: number; channel: string; destination: string; purpose: string; createdAt: string; expiresAt: string; attempts: number; isUsed: boolean; }
export interface NotificationLog { id: number; channel: string; recipient: string; body: string; status: string; createdAt: string; }
export interface AuditLog { id: number; action: string; details?: string; tenantId?: number; principalType?: string; principalId?: number; ipAddress?: string; createdAt: string; }

export interface Transliteration { input: string; tamil: string; words: { word: string; options: string[] }[]; source: string; }
