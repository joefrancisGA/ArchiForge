import { Skeleton } from "@/components/ui/skeleton";
import { WizardStepper } from "@/components/wizard/WizardStepper";

const MACRO_WIZARD_STEPS = [
  { label: "Request brief", description: "Identity, goals, starting point" },
  { label: "Constraints", description: "Requirements, policies, context" },
  { label: "Review and run", description: "Confirm, create, track" },
] as const;

/**
 * Suspense fallback for `/runs/new`: mirrors macro stepper placement without loading-copy text-only flash.
 */
export function NewRunWizardSkeleton() {
  return (
    <div aria-busy="true" className="mt-8 space-y-8" aria-label="Architecture request wizard">
      <WizardStepper steps={[...MACRO_WIZARD_STEPS]} currentStep={0} completedSteps={[]} />

      <div className="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-700 dark:bg-neutral-900">
        <div className="space-y-3">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-4 w-full max-w-lg" />
          <Skeleton className="h-4 w-full max-w-md" />
          <Skeleton className="h-32 w-full rounded-md" />
        </div>
      </div>
    </div>
  );
}
