export type SyncOperationStatus =
  | 'APPLIED'
  | 'ALREADY_APPLIED'
  | 'REJECTED'
  | 'CONFLICT'
  | 'REVIEW_REQUIRED'
  | 'DEPENDENCY_BLOCKED';

export type PreviewRecordKind = 'CFC' | 'SYNC';

export type PreviewStatusTone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

export interface ContingencyStatusCard {
  id: 'client-api' | 'dgi-provider' | 'formal-contingency' | 'sync-queue';
  label: string;
  value: string;
  detail: string;
  tone: PreviewStatusTone;
}

export interface PreviewHistoryEntry {
  at: string;
  title: string;
  detail: string;
}

export interface ContingencyPreviewRecord {
  id: string;
  kind: PreviewRecordKind;
  reference: string;
  source: string;
  createdAt: string;
  status: SyncOperationStatus;
  statusLabel: string;
  tone: PreviewStatusTone;
  summary: string;
  evidence: string;
  canonicalResult: string;
  recovery: string;
  history: PreviewHistoryEntry[];
}
