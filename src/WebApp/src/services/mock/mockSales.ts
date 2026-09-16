import type {
  SaleCreateInput,
  SaleDraftUpdateInput,
  SaleDto,
  SaleFiscalPreviewDto,
  SaleLineDto,
  SaleValidationDto,
} from '../../contracts/api';
import type { SalesGateway } from '../contracts';
import { mockItems } from './data';

interface MockSaleState {
  sale: SaleDto;
}

const sales = new Map<string, MockSaleState>();
let saleSequence = 1;
let lineSequence = 1;

const pause = <T,>(value: T) => new Promise<T>((resolve) => window.setTimeout(() => resolve(value), 180));

const nextGuid = (prefix: string, sequence: number) =>
  `${prefix}0000000-0000-4000-8000-${String(sequence).padStart(12, '0')}`;

const mapLines = (lines: SaleCreateInput['lines']): SaleLineDto[] =>
  lines.map((line) => {
    const item = mockItems.find((candidate) => candidate.id === line.itemId);
    if (!item) throw new Error(`Mock catalog item ${line.itemId} was not found.`);

    return {
      id: nextGuid('7', lineSequence++),
      itemId: item.id,
      itemCode: item.code,
      itemName: item.name,
      kind: item.kind,
      quantity: line.quantity,
      unitPrice: line.unitPrice,
      netAmount: line.quantity * line.unitPrice,
      taxProfileId: item.taxProfileId,
    };
  });

const netAmount = (lines: SaleLineDto[]) => lines.reduce((sum, line) => sum + line.netAmount, 0);

const findSale = (saleId: string) => {
  const state = sales.get(saleId);
  if (!state) throw new Error(`Mock sale ${saleId} was not found.`);
  return state;
};

const assertVersion = (sale: SaleDto, expectedVersion: number) => {
  if (sale.version !== expectedVersion) {
    throw new Error(`Mock version conflict. Expected ${expectedVersion}, current ${sale.version}.`);
  }
};

const buildPreview = (sale: SaleDto): SaleFiscalPreviewDto => ({
  saleId: sale.id,
  saleVersion: sale.version,
  currencyCode: sale.currencyCode,
  netAmount: sale.netAmount,
  previewTaxAmount: null,
  previewTotalAmount: null,
  lines: sale.lines.map((line) => ({
    lineId: line.id,
    itemCode: line.itemCode,
    netAmount: line.netAmount,
    taxTreatmentStatus: 'MOCK_UNRESOLVED',
    taxTreatment: 'SERVER_AUTHORITY_REQUIRED',
    treatmentCode: 'MOCK',
    taxRateStatus: 'MOCK_UNRESOLVED',
    vatLiability: 'MOCK_UNRESOLVED',
    vatRateKind: 'MOCK_UNRESOLVED',
    appliedRatePercent: null,
    previewTaxAmount: null,
    reasons: ['Demo mock: la forma del preview replica el contrato, pero el navegador no calcula fiscalidad.'],
    missingFacts: [],
    ruleReferences: [],
  })),
  overallTaxTreatmentStatus: 'MOCK_UNRESOLVED',
  overallTaxTreatment: 'SERVER_AUTHORITY_REQUIRED',
  treatmentCode: 'MOCK',
  cfe: {
    eligibilityStatus: 'MOCK_UNRESOLVED',
    selectionStatus: 'MOCK_NOT_SELECTED',
    selectedFamilyCode: null,
    selectedFamily: null,
    candidates: [],
    reasons: ['Demo mock: la selección de familia CFE permanece bajo autoridad del servidor.'],
    missingFacts: [],
    formatVersion: 'mock-demo-v1',
  },
  readyForValidation: true,
  validationFingerprint: sale.validationFingerprint ?? `mock-preview-${sale.id}-v${sale.version}`,
  findings: ['Preview de demostración API-shaped. Importes fiscales deliberadamente no resueltos.'],
  arithmeticAuthority: 'MOCK_DEMO_ONLY',
});

export const mockSalesGateway: SalesGateway = {
  async createSale(input) {
    const lines = mapLines(input.lines);
    const sale: SaleDto = {
      id: nextGuid('6', saleSequence++),
      version: 1,
      status: 'DRAFT',
      organizationId: 'mock-organization',
      locationId: input.locationId,
      terminalId: input.terminalId,
      customerPartyId: input.customerPartyId,
      intent: input.intent,
      currencyCode: input.currencyCode,
      effectiveOn: input.effectiveOn,
      deliveryCountry: input.deliveryCountry,
      netAmount: netAmount(lines),
      validationFingerprint: null,
      validatedAtUtc: null,
      lines,
    };

    sales.set(sale.id, { sale });
    return pause(sale);
  },

  async updateSaleDraft(saleId: string, input: SaleDraftUpdateInput) {
    const state = findSale(saleId);
    assertVersion(state.sale, input.expectedVersion);

    const lines = mapLines(input.lines);
    const sale: SaleDto = {
      ...state.sale,
      version: state.sale.version + 1,
      status: 'DRAFT',
      customerPartyId: input.customerPartyId,
      intent: input.intent,
      currencyCode: input.currencyCode,
      effectiveOn: input.effectiveOn,
      deliveryCountry: input.deliveryCountry,
      netAmount: netAmount(lines),
      validationFingerprint: null,
      validatedAtUtc: null,
      lines,
    };

    state.sale = sale;
    return pause(sale);
  },

  async validateSale(saleId: string, expectedVersion: number): Promise<SaleValidationDto> {
    const state = findSale(saleId);
    assertVersion(state.sale, expectedVersion);

    const version = state.sale.version + 1;
    const sale: SaleDto = {
      ...state.sale,
      version,
      status: 'VALIDATED',
      validationFingerprint: `mock-validation-${saleId}-v${version}`,
      validatedAtUtc: new Date().toISOString(),
    };
    state.sale = sale;

    return pause({
      valid: true,
      replayed: false,
      sale,
      preview: buildPreview(sale),
    });
  },

  async getSaleFiscalPreview(saleId: string) {
    return pause(buildPreview(findSale(saleId).sale));
  },
};
