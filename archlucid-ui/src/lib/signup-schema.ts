import { z } from "zod";

export const companySizeOptions = ["1-10", "11-50", "51-500", "501+"] as const;

export const BASELINE_REVIEW_CYCLE_HOURS_MAX = 10_000;
export const BASELINE_REVIEW_CYCLE_SOURCE_MAX = 256;

const baselineReviewCycleHoursSchema = z.preprocess(
  (raw) => {
    if (raw === undefined || raw === null) return undefined;
    if (typeof raw === "string") {
      const trimmed = raw.trim();
      if (trimmed.length === 0) return undefined;
      const n = Number(trimmed);
      return Number.isFinite(n) ? n : trimmed;
    }
    return raw;
  },
  z
    .number({ invalid_type_error: "Enter a numeric value (hours)." })
    .positive("Enter a number greater than 0.")
    .max(BASELINE_REVIEW_CYCLE_HOURS_MAX, `Enter a number at most ${BASELINE_REVIEW_CYCLE_HOURS_MAX}.`)
    .optional(),
);

const baselineReviewCycleSourceSchema = z.preprocess(
  (raw) => {
    if (typeof raw !== "string") return raw;
    const trimmed = raw.trim();
    return trimmed.length === 0 ? undefined : trimmed;
  },
  z
    .string()
    .max(BASELINE_REVIEW_CYCLE_SOURCE_MAX, `At most ${BASELINE_REVIEW_CYCLE_SOURCE_MAX} characters.`)
    .optional(),
);

export const signupFormSchema = z
  .object({
    adminEmail: z.string().trim().email("Enter a valid email."),
    adminDisplayName: z
      .string()
      .trim()
      .min(1, "Full name is required.")
      .max(200, "Full name must be at most 200 characters."),
    organizationName: z
      .string()
      .trim()
      .min(1, "Organization name is required.")
      .max(200, "Organization name must be at most 200 characters."),
    companySize: z.enum(companySizeOptions).optional(),
    baselineReviewCycleHours: baselineReviewCycleHoursSchema,
    baselineReviewCycleSource: baselineReviewCycleSourceSchema,
  })
  .refine(
    (v) => !(v.baselineReviewCycleSource !== undefined && v.baselineReviewCycleHours === undefined),
    {
      path: ["baselineReviewCycleHours"],
      message: "Enter the baseline hours when you provide a source note.",
    },
  );

export type SignupFormValues = z.infer<typeof signupFormSchema>;
