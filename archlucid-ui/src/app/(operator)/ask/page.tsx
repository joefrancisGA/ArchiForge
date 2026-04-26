"use client";

import { useCallback, useEffect, useState } from "react";

import { EmptyState } from "@/components/EmptyState";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
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
import { cn } from "@/lib/utils";
import type { ConversationMessage, ConversationThread } from "@/types/conversation";

export default function AskPage() {
  const [threads, setThreads] = useState<ConversationThread[]>([]);
  const [selectedThreadId, setSelectedThreadId] = useState("");
  const [messages, setMessages] = useState<ConversationMessage[]>([]);
  const [runId, setRunId] = useState("");
  const [baseRunId, setBaseRunId] = useState("");
  const [targetRunId, setTargetRunId] = useState("");
  const [question, setQuestion] = useState("");
  const [loading, setLoading] = useState(false);
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
        uiFailureFromMessage("Enter a run ID for a new conversation, or select an existing thread."),
      );
      return;
    }

    const base = baseRunId.trim();
    const target = targetRunId.trim();
    const useCompare = base.length > 0 && target.length > 0;
    if ((base.length > 0) !== (target.length > 0)) {
      setActionFailure(
        uiFailureFromMessage("Provide both base and target run IDs for comparison, or leave both empty."),
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
    await loadMessages(threadId);
  }

  return (
    <main className="max-w-5xl">
      <OperatorPageHeader
        title="Ask ArchLucid"
        helpKey="ask-archlucid"
        subtitle="Multi-turn conversations are scoped to your workspace. First message needs a run ID; follow-ups can use the same thread without resending it."
      />

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
            <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">Threads</CardTitle>
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
                      selectedThreadId === thread.threadId ? "font-semibold" : "font-normal",
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
              <div className="space-y-2">
                <Label htmlFor="ask-run-id">
                  Run ID {selectedThreadId ? "(optional if thread already anchored)" : "(required for new thread)"}
                </Label>
                <Input
                  id="ask-run-id"
                  className="font-mono text-sm"
                  value={runId}
                  onChange={(e) => setRunId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  autoComplete="off"
                />
              </div>
              <details className="rounded-md border border-neutral-200 bg-neutral-50/80 p-3 text-sm text-neutral-800 open:border-teal-600/40 dark:border-neutral-700 dark:bg-neutral-900/50 dark:text-neutral-200">
                <summary className="cursor-pointer font-medium text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-600 dark:text-neutral-100">
                  Optional: compare two runs
                </summary>
                <div className="mt-3 grid gap-3">
                  <div className="space-y-2">
                    <Label htmlFor="ask-base-run">Base run ID</Label>
                    <Input
                      id="ask-base-run"
                      className="font-mono text-sm"
                      value={baseRunId}
                      onChange={(e) => setBaseRunId(e.target.value)}
                      autoComplete="off"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="ask-target-run">Target run ID</Label>
                    <Input
                      id="ask-target-run"
                      className="font-mono text-sm"
                      value={targetRunId}
                      onChange={(e) => setTargetRunId(e.target.value)}
                      autoComplete="off"
                    />
                  </div>
                </div>
              </details>
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
              </div>

              <Button
                type="button"
                className="w-fit bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
                onClick={() => void onAsk()}
                disabled={loading || !question.trim()}
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
                      <p className="m-0 whitespace-pre-wrap text-sm text-neutral-800 dark:text-neutral-200">{message.content}</p>
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
