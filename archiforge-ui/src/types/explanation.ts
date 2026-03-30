/** AI-generated explanation of a single run's decisions, risks, costs, and compliance. */
export type RunExplanation = {
  summary: string;
  keyDrivers: string[];
  riskImplications: string[];
  costImplications: string[];
  complianceImplications: string[];
  detailedNarrative: string;
};

/** AI-generated narrative explaining the differences between two runs. */
export type ComparisonExplanation = {
  highLevelSummary: string;
  majorChanges: string[];
  keyTradeoffs: string[];
  narrative: string;
};
