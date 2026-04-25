"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { FormProvider, useForm } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";
import {
  BASELINE_REVIEW_CYCLE_HOURS_MAX,
  companySizeOptions,
  industryVerticalOptions,
  signupFormSchema,
  type BaselineSignupChoice,
  type SignupFormValues,
} from "@/lib/signup-schema";
import { showError, showSuccess } from "@/lib/toast";

type TenantProvisioningResult = {
  tenantId?: string;
  defaultWorkspaceId?: string;
  defaultProjectId?: string;
  wasAlreadyProvisioned?: boolean;
};

/** Self-service signup: posts to `POST /v1/register` via same-origin API proxy. */
export function SignupForm() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const form = useForm<SignupFormValues>({
    resolver: zodResolver(signupFormSchema),
    defaultValues: {
      adminEmail: "",
      adminDisplayName: "",
      organizationName: "",
      companySize: undefined,
      architectureTeamSize: "",
      industryVertical: undefined,
      industryVerticalOther: "",
      baselineChoice: "model_default",
      baselineReviewCycleHours: "",
      baselineReviewCycleSource: "",
    },
    mode: "onBlur",
  });

  const { register, handleSubmit, setValue, watch, formState } = form;
  const companySize = watch("companySize");
  const industryVertical = watch("industryVertical");
  const baselineChoice = watch("baselineChoice");

  function setBaselineChoice(next: BaselineSignupChoice): void {
    setValue("baselineChoice", next, { shouldValidate: true });

    if (next === "model_default") {
      setValue("baselineReviewCycleHours", "", { shouldValidate: true });
      setValue("baselineReviewCycleSource", "", { shouldValidate: true });
    }
  }

  const onSubmit = handleSubmit(async (values) => {
    setSubmitting(true);

    try {
      const payload: Record<string, unknown> = {
        organizationName: values.organizationName,
        adminEmail: values.adminEmail,
        adminDisplayName: values.adminDisplayName,
      };

      if (values.baselineChoice === "custom") {
        const hoursTrim = values.baselineReviewCycleHours?.trim() ?? "";

        if (hoursTrim.length > 0) {
          const hoursParsed = Number(hoursTrim);

          if (Number.isFinite(hoursParsed)) payload.baselineReviewCycleHours = hoursParsed;
        }

        const sourceTrim = values.baselineReviewCycleSource?.trim() ?? "";

        if (sourceTrim.length > 0) payload.baselineReviewCycleSource = sourceTrim;
      }

      if (values.companySize) {
        payload.companySize = values.companySize;
      }

      const teamTrim = values.architectureTeamSize?.trim() ?? "";
      if (teamTrim.length > 0) {
        const t = Number(teamTrim);
        if (Number.isFinite(t)) {
          payload.architectureTeamSize = t;
        }
      }

      if (values.industryVertical) {
        payload.industryVertical = values.industryVertical;
      }
      if (values.industryVertical === "Other") {
        const o = values.industryVerticalOther?.trim() ?? "";
        if (o.length > 0) {
          payload.industryVerticalOther = o;
        }
      }

      const res = await fetch("/api/proxy/v1/register", {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload),
      });

      const text = await res.text();
      let body: TenantProvisioningResult | { detail?: string } | null = null;

      try {
        body = text.length > 0 ? (JSON.parse(text) as TenantProvisioningResult) : null;
      } catch {
        body = null;
      }

      if (res.status === 409) {
        showError("Signup", "That organization name is already registered.");

        return;
      }

      if (!res.ok) {
        const detail =
          body && typeof body === "object" && "detail" in body && typeof body.detail === "string"
            ? body.detail
            : text || `Request failed (${res.status})`;
        showError("Signup", detail);

        return;
      }

      if (values.companySize) {
        try {
          sessionStorage.setItem("archlucid_signup_company_size", values.companySize);
        } catch {
          /* ignore */
        }
      }

      try {
        sessionStorage.setItem(
          "archlucid_last_registration",
          JSON.stringify({
            ...(body as TenantProvisioningResult),
            adminEmail: values.adminEmail,
            organizationName: values.organizationName,
          }),
        );
      } catch {
        /* ignore */
      }

      recordFirstTenantFunnelEvent("signup");
      showSuccess("Organization created — check your email if verification is required.");
      router.push(`/signup/verify?email=${encodeURIComponent(values.adminEmail)}`);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : "Request failed.";
      showError("Signup", message);
    } finally {
      setSubmitting(false);
    }
  });

  return (
    <FormProvider {...form}>
      <form className="mx-auto max-w-lg space-y-5" onSubmit={onSubmit} noValidate>
        <div>
          <Label htmlFor="signup-email">Work email</Label>
          <Input id="signup-email" type="email" autoComplete="email" {...register("adminEmail")} className="mt-1" />
          {formState.errors.adminEmail ? (
            <p className="mt-1 text-sm text-red-600" role="alert">
              {formState.errors.adminEmail.message}
            </p>
          ) : null}
        </div>

        <div>
          <Label htmlFor="signup-name">Full name</Label>
          <Input id="signup-name" autoComplete="name" {...register("adminDisplayName")} className="mt-1" />
          {formState.errors.adminDisplayName ? (
            <p className="mt-1 text-sm text-red-600" role="alert">
              {formState.errors.adminDisplayName.message}
            </p>
          ) : null}
        </div>

        <div>
          <Label htmlFor="signup-org">Organization name</Label>
          <Input id="signup-org" autoComplete="organization" {...register("organizationName")} className="mt-1" />
          {formState.errors.organizationName ? (
            <p className="mt-1 text-sm text-red-600" role="alert">
              {formState.errors.organizationName.message}
            </p>
          ) : null}
        </div>

        <div>
          <Label htmlFor="signup-size">Company size (optional)</Label>
          <Select
            value={companySize ?? "__none__"}
            onValueChange={(v) => {
              setValue("companySize", v === "__none__" ? undefined : (v as SignupFormValues["companySize"]), {
                shouldValidate: true,
              });
            }}
          >
            <SelectTrigger id="signup-size" className="mt-1">
              <SelectValue placeholder="Select range" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__none__">Prefer not to say</SelectItem>
              {companySizeOptions.map((opt) => (
                <SelectItem key={opt} value={opt}>
                  {opt} employees
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label htmlFor="signup-team-size">Architecture team size (optional)</Label>
          <Input
            id="signup-team-size"
            type="number"
            min={1}
            max={10_000}
            data-testid="signup-architecture-team-size"
            {...register("architectureTeamSize")}
            className="mt-1"
          />
          {formState.errors.architectureTeamSize ? (
            <p className="mt-1 text-sm text-red-600" role="alert">
              {formState.errors.architectureTeamSize.message}
            </p>
          ) : null}
        </div>

        <div>
          <Label htmlFor="signup-industry">Industry (optional)</Label>
          <Select
            value={industryVertical ?? "__ind_none__"}
            onValueChange={(v) => {
              setValue(
                "industryVertical",
                v === "__ind_none__" ? undefined : (v as SignupFormValues["industryVertical"]),
                { shouldValidate: true }
              );
            }}
          >
            <SelectTrigger id="signup-industry" className="mt-1" data-testid="signup-industry">
              <SelectValue placeholder="Select industry" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__ind_none__">Prefer not to say</SelectItem>
              {industryVerticalOptions.map((opt) => (
                <SelectItem key={opt} value={opt} data-testid={`signup-industry-${opt}`}>
                  {opt}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {industryVertical === "Other" ? (
          <div>
            <Label htmlFor="signup-industry-specify">Industry (specify)</Label>
            <Input
              id="signup-industry-specify"
              data-testid="signup-industry-specify"
              {...register("industryVerticalOther")}
              className="mt-1"
            />
            {formState.errors.industryVerticalOther ? (
              <p className="mt-1 text-sm text-red-600" role="alert">
                {formState.errors.industryVerticalOther.message}
              </p>
            ) : null}
          </div>
        ) : null}

        <div className="rounded-md border border-neutral-200 p-3 dark:border-neutral-700">
          <p id="signup-baseline-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Review-cycle baseline (recommended)
          </p>
          <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            Default keeps signup fast; we still compute a measured-vs-baseline curve after your first committed run using
            the conservative model from <code className="text-[0.85em]">docs/PILOT_ROI_MODEL.md</code> when you do not
            override.{" "}
            <strong>Supplying your own hours</strong> tightens the &quot;before&quot; anchor to your team&apos;s median
            review time. See the{" "}
            <a
              className="text-teal-800 underline dark:text-teal-300"
              href="https://github.com/joefrancisGA/ArchLucid/blob/main/docs/go-to-market/TRIAL_BASELINE_PRIVACY_NOTE.md"
              target="_blank"
              rel="noreferrer"
            >
              trial baseline privacy note
            </a>
            .
          </p>

          <fieldset className="mt-3 space-y-3" aria-labelledby="signup-baseline-heading">
            <legend className="sr-only">Baseline review-cycle</legend>
            <TooltipProvider delayDuration={200}>
              <div className="flex items-start gap-2">
                <input
                  id="signup-baseline-model"
                  type="radio"
                  className="mt-1"
                  checked={baselineChoice === "model_default"}
                  onChange={() => setBaselineChoice("model_default")}
                  data-testid="signup-baseline-choice-model"
                />
                <div>
                  <label htmlFor="signup-baseline-model" className="text-sm font-medium text-neutral-900 dark:text-neutral-100">
                    Use model default (modeled estimate)
                  </label>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <button
                        type="button"
                        className="ml-1 align-middle text-xs text-teal-800 underline dark:text-teal-300"
                        aria-label="Explain model default baseline"
                      >
                        What this means
                      </button>
                    </TooltipTrigger>
                    <TooltipContent className="max-w-xs text-xs leading-snug">
                      We record that you stayed on the modeled baseline path (no tenant-specific hours). The operator
                      dashboard still shows measured time-to-commit; the &quot;before&quot; line uses the conservative
                      default from the ROI model until you supply hours.
                    </TooltipContent>
                  </Tooltip>
                </div>
              </div>
            </TooltipProvider>

            <div className="flex items-start gap-2">
              <input
                id="signup-baseline-custom"
                type="radio"
                className="mt-1"
                checked={baselineChoice === "custom"}
                onChange={() => setBaselineChoice("custom")}
                data-testid="signup-baseline-choice-custom"
              />
              <label htmlFor="signup-baseline-custom" className="text-sm font-medium text-neutral-900 dark:text-neutral-100">
                I will enter our median review-cycle hours
              </label>
            </div>
          </fieldset>

          {baselineChoice === "custom" ? (
            <div className="mt-3 space-y-3 border-t border-neutral-200 pt-3 dark:border-neutral-600">
              <p className="text-xs text-neutral-600 dark:text-neutral-400">
                Overriding produces a measured-vs-baseline curve on the operator dashboard once a run commits.
              </p>
              <div>
                <Label htmlFor="signup-baseline-hours">Baseline review cycle (hours)</Label>
                <Input
                  id="signup-baseline-hours"
                  type="number"
                  inputMode="decimal"
                  min={0}
                  max={BASELINE_REVIEW_CYCLE_HOURS_MAX}
                  step="any"
                  data-testid="signup-baseline-hours"
                  {...register("baselineReviewCycleHours")}
                  className="mt-1"
                />
                {formState.errors.baselineReviewCycleHours ? (
                  <p className="mt-1 text-sm text-red-600" role="alert">
                    {formState.errors.baselineReviewCycleHours.message}
                  </p>
                ) : null}
              </div>
              <div>
                <Label htmlFor="signup-baseline-source">Source note (optional)</Label>
                <Input
                  id="signup-baseline-source"
                  data-testid="signup-baseline-source"
                  placeholder="e.g. team estimate; last 5 reviews"
                  {...register("baselineReviewCycleSource")}
                  className="mt-1"
                />
                {formState.errors.baselineReviewCycleSource ? (
                  <p className="mt-1 text-sm text-red-600" role="alert">
                    {formState.errors.baselineReviewCycleSource.message}
                  </p>
                ) : null}
              </div>
            </div>
          ) : null}
        </div>

        <div className="flex flex-wrap gap-3 pt-2">
          <Button type="submit" disabled={submitting} className="bg-teal-700 text-white hover:bg-teal-800">
            {submitting ? "Creating…" : "Create trial workspace"}
          </Button>
          <Button asChild type="button" variant="ghost">
            <Link href="/welcome">Back</Link>
          </Button>
        </div>

        <p className="text-xs text-neutral-600 dark:text-neutral-400">
          By continuing you agree to use ArchLucid in accordance with your organization&apos;s policies. The API applies
          rate limits on registration to protect shared infrastructure.
        </p>
      </form>
    </FormProvider>
  );
}
