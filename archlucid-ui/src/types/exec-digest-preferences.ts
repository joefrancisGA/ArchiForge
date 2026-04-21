export type ExecDigestPreferencesResponse = {
  schemaVersion: number;
  tenantId: string;
  isConfigured: boolean;
  emailEnabled: boolean;
  recipientEmails: string[];
  ianaTimeZoneId: string;
  dayOfWeek: number;
  hourOfDay: number;
  updatedUtc: string;
};

export type ExecDigestPreferencesUpsertRequest = {
  emailEnabled: boolean;
  recipientEmails: string[];
  ianaTimeZoneId: string;
  dayOfWeek: number;
  hourOfDay: number;
};
