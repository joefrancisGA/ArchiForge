"use client";

import { useCallback, useEffect, useState } from "react";
import {
  askArchiForge,
  getConversationMessages,
  listConversationThreads,
} from "@/lib/conversation-api";
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
  const [listError, setListError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const loadThreads = useCallback(async () => {
    setListError(null);
    try {
      const data = await listConversationThreads();
      setThreads(data);
    } catch (e) {
      setListError(e instanceof Error ? e.message : "Failed to load threads");
    }
  }, []);

  useEffect(() => {
    void loadThreads();
  }, [loadThreads]);

  async function loadMessages(threadId: string) {
    setActionError(null);
    try {
      const data = await getConversationMessages(threadId);
      setMessages(data);
    } catch (e) {
      setActionError(e instanceof Error ? e.message : "Failed to load messages");
    }
  }

  async function onAsk() {
    setActionError(null);
    const q = question.trim();
    if (!q) return;

    const rid = runId.trim();
    const tid = selectedThreadId.trim();
    if (!tid && !rid) {
      setActionError("Enter a run ID for a new conversation, or select an existing thread.");
      return;
    }

    const base = baseRunId.trim();
    const target = targetRunId.trim();
    const useCompare = base.length > 0 && target.length > 0;
    if ((base.length > 0) !== (target.length > 0)) {
      setActionError("Provide both base and target run IDs for comparison, or leave both empty.");
      return;
    }

    setLoading(true);
    try {
      const result = await askArchiForge({
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
      setActionError(e instanceof Error ? e.message : "Ask failed");
    } finally {
      setLoading(false);
    }
  }

  async function onSelectThread(threadId: string) {
    setSelectedThreadId(threadId);
    await loadMessages(threadId);
  }

  return (
    <main style={{ maxWidth: 1100 }}>
      <h2 style={{ marginTop: 0 }}>Ask ArchiForge</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Multi-turn conversations are scoped to your workspace. First message needs a <strong>run ID</strong>;
        follow-ups can use the same thread without resending it.
      </p>

      {listError ? (
        <p style={{ color: "crimson" }} role="alert">
          {listError}
        </p>
      ) : null}

      <div style={{ display: "grid", gridTemplateColumns: "280px 1fr", gap: 16 }}>
        <aside style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fff" }}>
          <h3 style={{ marginTop: 0 }}>Threads</h3>
          <button
            type="button"
            onClick={() => {
              setSelectedThreadId("");
              setMessages([]);
            }}
            style={{ marginBottom: 12, fontSize: 13 }}
          >
            New conversation
          </button>
          <ul style={{ paddingLeft: 16, margin: 0, listStyle: "disc" }}>
            {threads.map((thread) => (
              <li key={thread.threadId} style={{ marginBottom: 8 }}>
                <button
                  type="button"
                  onClick={() => onSelectThread(thread.threadId)}
                  style={{
                    textAlign: "left",
                    fontWeight: selectedThreadId === thread.threadId ? "bold" : "normal",
                  }}
                >
                  {thread.title}
                  <div style={{ fontSize: 11, color: "#666", fontWeight: "normal" }}>
                    {new Date(thread.lastUpdatedUtc).toLocaleString()}
                  </div>
                </button>
              </li>
            ))}
          </ul>
        </aside>

        <section style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fff" }}>
          <div style={{ display: "grid", gap: 12, marginBottom: 16 }}>
            <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 14 }}>
              Run ID {selectedThreadId ? "(optional if thread already anchored)" : "(required for new thread)"}
              <input
                value={runId}
                onChange={(e) => setRunId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                style={{ padding: 8, fontFamily: "monospace" }}
              />
            </label>
            <details style={{ fontSize: 14 }}>
              <summary style={{ cursor: "pointer" }}>Optional: compare two runs</summary>
              <div style={{ display: "flex", flexDirection: "column", gap: 12, marginTop: 12 }}>
                <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                  Base run ID
                  <input
                    value={baseRunId}
                    onChange={(e) => setBaseRunId(e.target.value)}
                    style={{ padding: 8, fontFamily: "monospace" }}
                  />
                </label>
                <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                  Target run ID
                  <input
                    value={targetRunId}
                    onChange={(e) => setTargetRunId(e.target.value)}
                    style={{ padding: 8, fontFamily: "monospace" }}
                  />
                </label>
              </div>
            </details>
            <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 14 }}>
              Question
              <textarea
                value={question}
                onChange={(e) => setQuestion(e.target.value)}
                placeholder="Ask about your architecture..."
                rows={4}
                style={{ padding: 8, fontFamily: "inherit" }}
              />
            </label>

            <button type="button" onClick={() => void onAsk()} disabled={loading || !question.trim()}>
              {loading ? "Thinking…" : "Ask"}
            </button>
          </div>

          {actionError ? (
            <p style={{ color: "crimson", marginBottom: 16 }} role="alert">
              {actionError}
            </p>
          ) : null}

          <h3 style={{ marginTop: 0 }}>Conversation</h3>
          <div style={{ display: "grid", gap: 12 }}>
            {messages.length === 0 ? (
              <p style={{ color: "#666", fontSize: 14 }}>No messages yet. Ask a question to start.</p>
            ) : null}
            {messages.map((message) => (
              <div
                key={message.messageId}
                style={{
                  border: "1px solid #eee",
                  borderRadius: 8,
                  padding: 12,
                  background: message.role === "User" ? "#eef6ff" : "#f8f8f8",
                }}
              >
                <strong>{message.role}</strong>
                <p style={{ whiteSpace: "pre-wrap", marginBottom: 0 }}>{message.content}</p>
              </div>
            ))}
          </div>
        </section>
      </div>
    </main>
  );
}
