import type { CSSProperties } from "react";

export const planningTableStyle: CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  fontSize: 14,
  marginTop: 8,
};

export const planningThTd: CSSProperties = {
  border: "1px solid #e2e8f0",
  padding: "8px 10px",
  textAlign: "left",
  verticalAlign: "top",
};

export const planningNumericCell: CSSProperties = {
  ...planningThTd,
  textAlign: "right",
  fontVariantNumeric: "tabular-nums",
};
