# API Completion Master Matrix — Wave 5

Status: `FULL_OPERATION_LEVEL_RECONCILED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Fiscal Completion + CAE + CFE Lifecycle**.

Baseline: **40 operation IDs**, **8 implemented**, **32 non-implemented**.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-FIS-001` | `listFiscalDocuments` | GET `/api/v1/fiscal-documents` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Fiscal-document query surface required |
| `API-FIS-002` | `getFiscalDocument` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Fiscal-document detail projection required |
| `API-FIS-003` | `downloadFiscalXml` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/xml` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Authorized immutable XML download required |
| `API-FIS-004` | `downloadFiscalRepresentation` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/representation` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Printable representation surface required |
| `API-FIS-005` | `listFiscalDocumentEvents` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/events` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Fiscal lifecycle timeline projection required |
| `API-FIS-006` | `createFiscalCorrection` | POST `/api/v1/fiscal-documents/{fiscalDocumentId}/corrections` | `fiscal.correct` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Governed correction command required |
| `API-FIS-007` | `listRegularizationCases` | GET `/api/v1/fiscal-regularizations` | `fiscal.regularization.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Regularization work queue required |
| `API-FIS-008` | `getRegularizationCase` | GET `/api/v1/fiscal-regularizations/{caseId}` | `fiscal.regularization.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Regularization case/evidence projection required |
| `API-FIS-009` | `resolveRegularizationCase` | POST `/api/v1/fiscal-regularizations/{caseId}/resolve` | `fiscal.regularization.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Governed regularization disposition required |
| `API-FIS-010` | `collectFiscalEnvelopeDocumentResponseEvidence` | POST `/api/v1/fiscal-envelopes/{envelopeId}/document-response-evidence` | `fiscal.regularization.manage` | ACCEPTED | IMPLEMENTED | `FiscalCfeEnvelopesController` | EXISTING_PATH / regression | 5 | Preserve fail-closed evidence semantics and regression-test |
| `API-FDL-001` | `listFiscalDocumentDeliveries` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/deliveries` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Delivery-attempt projection required |
| `API-FDL-002` | `requestFiscalDocumentDelivery` | POST `/api/v1/fiscal-documents/{fiscalDocumentId}/deliveries` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Delivery request command required |
| `API-CAE-001` | `listCaeAuthorizations` | GET `/api/v1/cae-authorizations` | `fiscal.read` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-002` | `getCaeAuthorization` | GET `/api/v1/cae-authorizations/{caeId}` | `fiscal.read` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-003` | `importCaeAuthorization` | POST `/api/v1/cae-authorizations/import` | `fiscal.manage_cae` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-004` | `activateCaeAuthorization` | POST `/api/v1/cae-authorizations/{caeId}/activate` | `fiscal.manage_cae` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-005` | `listCaeAllocations` | GET `/api/v1/cae-authorizations/{caeId}/allocations` | `fiscal.read` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-006` | `createCaeAllocation` | POST `/api/v1/cae-authorizations/{caeId}/allocations` | `fiscal.manage_cae` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CAE-007` | `closeCaeAllocation` | POST `/api/v1/cae-authorizations/{caeId}/allocations/{allocationId}/close` | `fiscal.manage_cae` | ACCEPTED | IMPLEMENTED | `CaeAuthorizationsController` | EXISTING_PATH / regression | 5 | Preserve and regression-test |
| `API-CNT-001` | `getContingencyStatus` | GET `/api/v1/contingency/status` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Formal contingency state surface required |
| `API-CNT-002` | `enterContingency` | POST `/api/v1/contingency/enter` | `fiscal.manage_contingency` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Contingency entry command required |
| `API-CNT-003` | `exitContingency` | POST `/api/v1/contingency/exit` | `fiscal.manage_contingency` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Contingency exit/recovery boundary required |
| `API-CNT-004` | `listContingencyDocuments` | GET `/api/v1/contingency/documents` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Contingency-document projection required |
| `API-CNT-005` | `registerContingencyDocument` | POST `/api/v1/contingency/documents` | `fiscal.manage_contingency` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | CFC registration preserving original identity required |
| `API-CNT-006` | `getContingencyDocument` | GET `/api/v1/contingency/documents/{contingencyDocumentId}` | `fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | CFC recovery/reporting projection required |
| `API-CNT-007` | `reconcileContingencyDocument` | POST `/api/v1/contingency/documents/{contingencyDocumentId}/reconcile` | `fiscal.manage_contingency` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Permitted recovery/report linkage required |
| `API-RCV-001` | `listReceivedFiscalDocuments` | GET `/api/v1/received-fiscal-documents` | `received_fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Received-fiscal document query required |
| `API-RCV-002` | `importReceivedFiscalDocument` | POST `/api/v1/received-fiscal-documents/import` | `received_fiscal.import` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Single artifact import + duplicate detection required |
| `API-RCV-003` | `importReceivedFiscalDocumentsBatch` | POST `/api/v1/received-fiscal-documents/import-batch` | `received_fiscal.import` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Bounded batch import required |
| `API-RCV-004` | `getReceivedFiscalDocument` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}` | `received_fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Received document detail projection required |
| `API-RCV-005` | `downloadReceivedFiscalArtifact` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}/artifact` | `received_fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Original artifact authorized download required |
| `API-RCV-006` | `listReceivedFiscalValidationFindings` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}/findings` | `received_fiscal.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Structured validation findings projection required |
| `API-XML-001` | `validateFiscalXml` | POST `/api/v1/fiscal-validation/xml` | `received_fiscal.validate` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Non-canonical XML validation surface required |
| `API-DFR-001` | `listDailyFiscalReports` | GET `/api/v1/fiscal-reports/daily` | `fiscal.report.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Statutory daily-report query required |
| `API-DFR-002` | `getDailyFiscalReport` | GET `/api/v1/fiscal-reports/daily/{reportId}` | `fiscal.report.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Daily-report lifecycle projection required |
| `API-DFR-003` | `generateDailyFiscalReport` | POST `/api/v1/fiscal-reports/daily/generate` | `fiscal.report.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Daily-report generation boundary required |
| `API-DFR-004` | `submitDailyFiscalReport` | POST `/api/v1/fiscal-reports/daily/{reportId}/submit` | `fiscal.report.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Statutory report submission boundary required |
| `API-CAL-001` | `getFiscalCalendar` | GET `/api/v1/fiscal-calendar` | `reports.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Sourced fiscal calendar projection required |
| `API-CFG-001` | `getFiscalConfiguration` | GET `/api/v1/configuration/fiscal` | `fiscal.configuration.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Safe effective fiscal configuration projection required |
| `API-CFG-002` | `updateFiscalConfiguration` | PATCH `/api/v1/configuration/fiscal` | `fiscal.configuration.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 5 | Approved non-secret fiscal configuration update required |
