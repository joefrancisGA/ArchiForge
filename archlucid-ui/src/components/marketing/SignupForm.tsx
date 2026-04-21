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
import {
  BASELINE_REVIEW_CYCLE_HOURS_MAX,
  companySizeOptions,
  signupFormSchema,
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
  const [baselineExpanded, setBaselineExpanded] = useState(false);
  const form = useForm<SignupFormValues>({
    resolver: zodResolver(signupFormSchema),
    defaultValues: {
      adminEmail: "",
      adminDisplayName: "",
      organizationName: "",
      companySize: undefined,
      baselineReviewCycleHours: undefined,
      baselineReviewCycleSource: undefined,
    },
    mode: "onBlur",
  });

  const { register, handleSubmit, setValue, watch, formState } = form;
  const companySize = watch("companySize");

  const onSubmit = handleSubmit(async (values) => {
    setSubmitting(true);

    try {
      const payload: Record<string, unknown> = {
        organizationName: values.organizationName,
        adminEmail: values.adminEmail,
        adminDisplayName: values.adminDisplayName,
      };

      if (values.baselineReviewCycleHours !== undefined) {
        payload.baselineReviewCycleHours = values.baselineReviewCycleHours;
      }

      if (values.baselineReviewCycleSource !== undefined) {
        payload.baselineReviewCycleSource = values.baselineReviewCycleSource;
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

        <div className="rounded-md border border-neutral-200 p-3 dark:border-neutral-700">
          <button
            type="button"
            onClick={() => setBaselineExpanded((prev) => !prev)}
            aria-expanded={baselineExpanded}
            aria-controls="signup-baseline-fields"
            data-testid="signup-baseline-disclosure"
            className="text-left text-sm font-medium text-teal-800 hover:underline dark:text-teal-300"
          >
            {baselineExpanded ? "▾" : "▸"} Add a baseline review-cycle estimate (optional)
          </button>
          <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            If your team has a rough median (in hours) for an architecture review today, we will compare it to your
            measured time on the operator dashboard once your first run commits. Skip this if you do not know — we will
            use a conservative model default and label it accordingly.
          </p>
          {baselineExpanded ? (
            <div id="signup-baseline-fields" className="mt-3 space-y-3">
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
