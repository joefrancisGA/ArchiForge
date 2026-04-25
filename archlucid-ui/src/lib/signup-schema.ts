import { z } from "zod";

export const companySizeOptions = [
  "1-10",
  "11-50",
  "51-200",
  "201-1000",
  "1001-5000",
  "5001-50000",
  "50001+"
] as const;

export const industryVerticalOptions = [
  "Healthcare",
  "Financial Services",
  "Technology",
  "Government / Public Sector",
  "Manufacturing",
  "Retail",
  "Insurance",
  "Energy / Utilities",
  "Education",
  "Telecommunications",
  "Other"
] as const;

export const BASELINE_REVIEW_CYCLE_HOURS_MAX = 10_000;
export const BASELINE_REVIEW_CYCLE_SOURCE_MAX = 256;

export const baselineSignupChoiceValues = ["model_default", "custom"] as const;
export type BaselineSignupChoice = (typeof baselineSignupChoiceValues)[number];

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
    architectureTeamSize: z.string().optional(),
    industryVertical: z.enum(industryVerticalOptions).optional(),
    industryVerticalOther: z.string().max(200, "At most 200 characters.").optional(),
    baselineChoice: z.enum(baselineSignupChoiceValues),
    // String form values keep `z.infer` stable for `zodResolver` + react-hook-form (avoid `z.preprocess` → `unknown`).
    baselineReviewCycleHours: z.string().optional(),
    baselineReviewCycleSource: z.string().optional()
  })
  .superRefine((v, ctx) => {
    const arch = v.architectureTeamSize?.trim() ?? "";
    if (arch.length > 0) {
      const n = Number(arch);
      if (!Number.isFinite(n)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Enter a valid number for architecture team size.",
          path: ["architectureTeamSize"]
        });
      } else if (n <= 0 || n > 10_000) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Architecture team size must be between 1 and 10,000 when provided.",
          path: ["architectureTeamSize"]
        });
      }
    }

    if (v.industryVertical === "Other" && (v.industryVerticalOther == null || v.industryVerticalOther.trim().length === 0)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Please specify your industry when you select “Other.”",
        path: ["industryVerticalOther"]
      });
    }

    if (v.baselineChoice !== "custom") {
      return;
    }

    const hoursRaw = v.baselineReviewCycleHours?.trim() ?? "";

    if (hoursRaw.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Enter your team's median review-cycle hours, or switch back to the model default.",
        path: ["baselineReviewCycleHours"]
      });

      return;
    }

    const parsed = Number(hoursRaw);

    if (!Number.isFinite(parsed)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Enter a numeric value (hours).",
        path: ["baselineReviewCycleHours"]
      });

      return;
    }

    if (parsed <= 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Enter a number greater than 0.",
        path: ["baselineReviewCycleHours"]
      });

      return;
    }

    if (parsed > BASELINE_REVIEW_CYCLE_HOURS_MAX) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: `Enter a number at most ${BASELINE_REVIEW_CYCLE_HOURS_MAX}.`,
        path: ["baselineReviewCycleHours"]
      });

      return;
    }

    const sourceTrimmed = v.baselineReviewCycleSource?.trim() ?? "";

    if (sourceTrimmed.length > BASELINE_REVIEW_CYCLE_SOURCE_MAX) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: `At most ${BASELINE_REVIEW_CYCLE_SOURCE_MAX} characters.`,
        path: ["baselineReviewCycleSource"]
      });
    }
  });

export type SignupFormValues = z.infer<typeof signupFormSchema>;
