import { AUTH_MODE } from "@/lib/auth-config";

export function AuthStatus() {
  const label =
    AUTH_MODE === "development-bypass"
      ? "Development bypass (API auto-authenticates; no UI sign-in)"
      : AUTH_MODE === "jwt" || AUTH_MODE === "jwt-bearer"
        ? "JWT / OIDC (wire bearer tokens in api.ts when ready)"
        : AUTH_MODE;

  return (
    <div
      style={{
        padding: 12,
        border: "1px solid #ddd",
        borderRadius: 8,
        marginBottom: 16,
        background: "#fff",
      }}
    >
      <strong>Auth mode:</strong> {label}
    </div>
  );
}
