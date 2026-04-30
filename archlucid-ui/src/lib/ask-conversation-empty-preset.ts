import { MessageCircle } from "lucide-react";

import type { EmptyStateProps } from "@/components/EmptyState";

/** Shown in Ask ArchLucid when a thread has no messages yet. */
export const ASK_CONVERSATION_EMPTY: EmptyStateProps = {
  icon: MessageCircle,
  title: "No messages yet",
  description:
    "Select an architecture review for a new conversation (or open one on the left), then ask a question. Follow-ups reuse the same thread without picking the review again.",
};
