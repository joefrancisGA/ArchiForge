"use client";

import { useCallback, useEffect, useState } from "react";

import { ChevronDown } from "lucide-react";

import { AskRunIdPicker } from "@/components/AskRunIdPicker";
import { AskAssistantMessageBody } from "@/components/AskAssistantMessageBody";
import { EmptyState } from "@/components/EmptyState";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { useWorkspaceActiveRun } from "@/components/WorkspaceActiveRunContext";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import {
  askArchLucid,
  getConversationMessages,
  listConversationThreads,
} from "@/lib/conversation-api";
import { ASK_CONVERSATION_EMPTY } from "@/lib/ask-conversation-empty-preset";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import { cn } from "@/lib/utils";
import type { ConversationMessage, ConversationThread } from "@/types/conversation";

const ASK_EXAMPLE_PROMPTS: readonly string[] = [
  "Summarize the PHI risk for this run.",
  "What should the sponsor review before sign-off?",
  "Explain the finalized manifest in plain language.",
  "Summarize the finalized manifest for a sponsor.",
  "What evidence supports the PHI minimization risk?",
  "Summarize this for an executive sponsor.",
];

export default function AskPage() {
  const workspaceRun = useWorkspaceActiveRun();
  const [threads, setThreads] = useState<ConversationThread[]>([]);
  const [selectedThreadId, setSelectedThreadId] = useState("");
  const [messages, setMessages] = useState<ConversationMessage[]>([]);
  const [runId, setRunId] = useState("");
  const [baseRunId, setBaseRunId] = useState("");
  const [targetRunId, setTargetRunId] = useState("");
  const [question, setQuestion] = useState("");
  const [loading, setLoading] = useState(false);
  const [compareOpen, setCompareOpen] = useState(false);
  const [listFailure, setListFailure] = useState<ApiLoadFailureState | null>(null);
  const [actionFailure, setActionFailure] = useState<ApiLoadFailureState | null>(null);

  const loadThreads = useCallback(async () => {
    setListFailure(null);
    try {
      const data = await listConversationThreads();
      setThreads(data);
    } catch (e) {
      setListFailure(toApiLoadFailure(e));
    }
  }, []);

  useEffect(() => {
    void loadThreads();
  }, [loadThreads]);

  useEffect(() => {
    const fromWorkspace = workspaceRun?.activeRunId?.trim() ?? "";

    if (selectedThreadId.trim().length > 0) {
      return;
    }

    if (fromWorkspace.length === 0) {
      return;
    }

    if (runId.trim().length > 0) {
      return;
    }

    setRunId(fromWorkspace);
  }, [workspaceRun?.activeRunId, selectedThreadId, runId]);

  useEffect(() => {
    if (!isNextPublicDemoMode()) {
      return;
    }

    if (selectedThreadId.trim().length > 0) {
      return;
    }

    if (runId.trim().length > 0) {
      return;
    }

    setRunId(SHOWCASE_STATIC_DEMO_RUN_ID);
  }, [selectedThreadId, runId]);

  async function loadMessages(threadId: string) {
    setActionFailure(null);
    try {
      const data = await getConversationMessages(threadId);
      setMessages(data);
    } catch (e) {
      setActionFailure(toApiLoadFailure(e));
    }
  }

  async function onAsk() {
    setActionFailure(null);
    const q = question.trim();
    if (!q) return;

    const rid = runId.trim();
    const tid = selectedThreadId.trim();
    if (!tid && !rid) {
      setActionFailure(
        uiFailureFromMessage("Choose a run for a new thread, or open an existing thread from the left."),
      );
      return;
    }

    const base = baseRunId.trim();
    const target = targetRunId.trim();
    const useCompare = base.length > 0 && target.length > 0;
    if ((base.length > 0) !== (target.length > 0)) {
      setActionFailure(
        uiFailureFromMessage("Provide both baseline and updated runs for comparison, or leave both empty."),
      );
      return;
    }

    setLoading(true);
    try {
      const result = await askArchLucid({
        threadId: tid || undefined,
        runId: rid || undefined,
        question: q,
        baseRunId: useCompare ? base : undefined,
        targetRunId: useCompare ? target : undefined,
      });

      setSelectedThreadId(result.threadId);
      setQuestion("");
      await loadThreads();
      await loadMessages(result.threadId);
    } catch (e) {
      setActionFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function onSelectThread(threadId: string) {
    setSelectedThreadId(threadId);

    const thread = threads.find((t) => t.threadId === threadId);

    if (thread?.runId) {
      setRunId(thread.runId);
    } else {
      setRunId("");
    }

    if (thread?.baseRunId) {
      setBaseRunId(thread.baseRunId);
      setTargetRunId(thread.targetRunId ?? "");
      setCompareOpen(true);
    } else {
      setBaseRunId("");
      setTargetRunId("");
      setCompareOpen(false);
    }

    await loadMessages(threadId);
  }

  const threadSelected = selectedThreadId.trim().length > 0;
  const needsRunForNewThread = !threadSelected;
  const runMissing = needsRunForNewThread && runId.trim().length === 0;
  const askDisabled = loading || question.trim().length === 0 || runMissing;

  return (
    <main className="max-w-5xl">
      <OperatorPageHeader
        title="Ask ArchLucid"
        helpKey="ask-archlucid"
        subtitle="Multi-turn conversations stay in your workspace. Pick a run for a new thread; follow-ups stay on the same thread without picking the run again."
      />
      <p className="mb-4 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
        Answers use the run context you select (finalized manifest and findings when available; in-progress runs may
        omit late-stage outputs until the pipeline completes).
      </p>

      {listFailure !== null ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={listFailure.problem}
            fallbackMessage={listFailure.message}
            correlationId={listFailure.correlationId}
          />
        </div>
      ) : null}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-[minmax(220px,280px)_1fr]">
        <Card className="h-fit border-neutral-200 dark:border-neutral-700">
          <CardHeader className="p-4 pb-2">
            <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
              Your conversation history
            </CardTitle>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Threads for your signed-in workspace user. They are not marketing sample dialogs — start{" "}
              <strong>New conversation</strong> and pick a run, or open a thread to continue with its saved run context.
            </p>
          </CardHeader>
          <CardContent className="space-y-3 p-4 pt-0">
            <Button
              type="button"
              variant="outline"
              className="w-full border-neutral-300 text-neutral-800 hover:bg-neutral-100 dark:border-neutral-600 dark:text-neutral-200 dark:hover:bg-neutral-800"
              onClick={() => {
                setSelectedThreadId("");
                setMessages([]);
              }}
            >
              New conversation
            </Button>
            <ul className="m-0 list-none space-y-1 p-0">
              {threads.map((thread) => (
                <li key={thread.threadId}>
                  <Button
                    type="button"
                    variant="ghost"
                    className={cn(
                      "h-auto w-full justify-start whitespace-normal py-2 text-left text-sm",
                      selectedThreadId === thread.threadId &&
                        "border border-teal-300 bg-teal-50/80 font-semibold dark:border-teal-700 dark:bg-teal-950/40",
                      selectedThreadId !== thread.threadId && "font-normal",
                    )}
                    onClick={() => void onSelectThread(thread.threadId)}
                  >
                    <span>
                      {thread.title}
                      <div className="text-xs font-normal text-neutral-500 dark:text-neutral-500">
                        {new Date(thread.lastUpdatedUtc).toLocaleString()}
                      </div>
                    </span>
                  </Button>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-700">
          <CardContent className="space-y-4 p-4">
            <div className="grid gap-3">
              <AskRunIdPicker
                value={runId}
                onChange={setRunId}
                selectedThreadId={selectedThreadId}
                fieldId="ask-run-primary"
              />
              <Collapsible open={compareOpen} onOpenChange={setCompareOpen}>
                <div className="rounded-md border border-neutral-200 bg-neutral-50/80 p-3 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/50 dark:text-neutral-200">
                  <CollapsibleTrigger asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      className="h-auto w-full justify-between gap-2 p-0 font-medium text-neutral-900 hover:bg-transparent dark:text-neutral-100"
                      aria-expanded={compareOpen}
                    >
                      <span>Compare against another run</span>
                      <ChevronDown
                        className={cn(
                          "h-4 w-4 shrink-0 text-neutral-600 transition-transform dark:text-neutral-400",
                          compareOpen && "rotate-180",
                        )}
                        aria-hidden
                      />
                    </Button>
                  </CollapsibleTrigger>
                  <CollapsibleContent className="mt-3 grid gap-3">
                    <AskRunIdPicker
                      value={baseRunId}
                      onChange={setBaseRunId}
                      selectedThreadId={selectedThreadId}
                      preferAutoPick={false}
                      label="Base run"
                      fieldId="ask-compare-base"
                    />
                    <AskRunIdPicker
                      value={targetRunId}
                      onChange={setTargetRunId}
                      selectedThreadId={selectedThreadId}
                      preferAutoPick={false}
                      label="Compare run"
                      fieldId="ask-compare-target"
                    />
                  </CollapsibleContent>
                </div>
              </Collapsible>
              <div className="space-y-2">
                <Label htmlFor="ask-question">Question</Label>
                <Textarea
                  id="ask-question"
                  className="min-h-[5rem] font-sans"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  placeholder="Ask about your architecture..."
                  rows={4}
                />
                <div className="flex flex-wrap gap-2" role="group" aria-label="Example prompts">
                  {ASK_EXAMPLE_PROMPTS.map((line) => (
                    <Button
                      key={line}
                      type="button"
                      variant="outline"
                      size="sm"
                      className="h-auto max-w-full whitespace-normal py-1.5 text-left text-xs font-normal"
                      onClick={() => setQuestion(line)}
                    >
                      {line}
                    </Button>
                  ))}
                </div>
              </div>

              <Button
                type="button"
                variant="primary"
                className="w-fit"
                onClick={() => void onAsk()}
                disabled={askDisabled}
              >
                {loading ? "Thinking…" : "Ask"}
              </Button>
            </div>

            {actionFailure !== null ? (
              <div role="alert" className="pt-0">
                <OperatorApiProblem
                  problem={actionFailure.problem}
                  fallbackMessage={actionFailure.message}
                  correlationId={actionFailure.correlationId}
                />
              </div>
            ) : null}

            <div>
              <h3 className="mb-3 m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Conversation</h3>
              <div className="grid gap-3">
                {messages.length === 0 ? (
                  <EmptyState {...ASK_CONVERSATION_EMPTY} />
                ) : null}
                {messages.map((message) => (
                  <Card
                    key={message.messageId}
                    className={cn(
                      "border",
                      message.role === "User"
                        ? "border-sky-200/90 bg-sky-50/90 dark:border-sky-800/80 dark:bg-sky-950/35"
                        : "border-neutral-200 bg-neutral-50/90 dark:border-neutral-700 dark:bg-neutral-800/50",
                    )}
                  >
                    <CardContent className="space-y-1 p-3">
                      <div className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">{message.role}</div>
                      {message.role.toLowerCase() === "assistant" ? (
                        <AskAssistantMessageBody content={message.content} />
                      ) : (
                        <p className="m-0 whitespace-pre-wrap text-sm text-neutral-800 dark:text-neutral-200">
                          {message.content}
                        </p>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </main>
  );
}
