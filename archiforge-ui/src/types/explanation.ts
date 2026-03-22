export type RunExplanation = {
  summary: string;
  keyDrivers: string[];
  riskImplications: string[];
  costImplications: string[];
  complianceImplications: string[];
  detailedNarrative: string;
};

export type ComparisonExplanation = {
  highLevelSummary: string;
  majorChanges: string[];
  keyTradeoffs: string[];
  narrative: string;
};
