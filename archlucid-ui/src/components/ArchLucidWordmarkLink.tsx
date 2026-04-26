"use client";

import Link, { type LinkProps } from "next/link";
import { forwardRef, type AnchorHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

export type ArchLucidWordmarkLinkProps = Omit<LinkProps, "children"> &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "children" | "href"> & {
  variant: "operator" | "marketing";
  "aria-label": string;
};

/**
 * Header wordmark: light/dark SVG pair from /public/logo (img tags so Inter in SVG is not required).
 * Use inside <Button asChild> so Radix merges focus styles onto the anchor.
 */
export const ArchLucidWordmarkLink = forwardRef<HTMLAnchorElement, ArchLucidWordmarkLinkProps>(
  function ArchLucidWordmarkLink({ variant, className, ...linkProps }, ref) {
    const heightClass = variant === "operator" ? "h-8" : "h-7";

    return (
      <Link
        ref={ref}
        {...linkProps}
        className={cn("inline-flex shrink-0 items-center focus:outline-none", heightClass, className)}
      >
        <img
          src="/logo/archlucid.svg"
          alt=""
          width={220}
          height={60}
          className={cn(heightClass, "w-auto dark:hidden")}
          decoding="async"
        />

        <img
          src="/logo/archlucid-dark.svg"
          alt=""
          width={220}
          height={60}
          className={cn("hidden", heightClass, "w-auto dark:block")}
          decoding="async"
        />
      </Link>
    );
  },
);
