namespace EFactura.Application.Common.Security;

public static class Permissions
{
    public const string SecurityUsersRead = "security.users.read";
    public const string SecurityUsersManage = "security.users.manage";
    public const string SecurityRolesRead = "security.roles.read";
    public const string SecurityManageRoles = "security.manage_roles";
    public const string OrganizationRead = "organization.read";
    public const string OrganizationManage = "organization.manage";
    public const string PartiesRead = "parties.read";
    public const string PartiesManage = "parties.manage";
    public const string PartiesFiscalManage = "parties.fiscal.manage";
    public const string CatalogRead = "catalog.read";
    public const string CatalogManage = "catalog.manage";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsManage = "payments.manage";
    public const string DashboardRead = "dashboard.read";
    public const string SalesRead = "sales.read";
    public const string SalesCreate = "sales.create";
    public const string SalesConfirm = "sales.confirm";
    public const string SalesCancel = "sales.cancel";
    public const string FiscalRead = "fiscal.read";
    public const string FiscalCorrect = "fiscal.correct";
    public const string FiscalRegularizationManage = "fiscal.regularization.manage";
    public const string FiscalManageCae = "fiscal.manage_cae";
    public const string FiscalManageContingency = "fiscal.manage_contingency";
    public const string FiscalReportRead = "fiscal.report.read";
    public const string FiscalReportManage = "fiscal.report.manage";
    public const string FiscalConfigurationRead = "fiscal.configuration.read";
    public const string FiscalConfigurationManage = "fiscal.configuration.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryAdjust = "inventory.adjust";
    public const string InventoryTransfer = "inventory.transfer";
    public const string ProcurementRead = "procurement.read";
    public const string ProcurementManage = "procurement.manage";
    public const string ProcurementApprove = "procurement.approve";
    public const string ProcurementReceive = "procurement.receive";
    public const string ReceivablesRead = "receivables.read";
    public const string ReceivablesAdjust = "receivables.adjust";
    public const string ReceivablesCollect = "receivables.collect";
    public const string PayablesRead = "payables.read";
    public const string PayablesAdjust = "payables.adjust";
    public const string PayablesPay = "payables.pay";
    public const string CashRead = "cash.read";
    public const string CashOpen = "cash.open";
    public const string CashMove = "cash.move";
    public const string CashClose = "cash.close";
    public const string CashReconcile = "cash.reconcile";
    public const string SyncUse = "sync.use";
    public const string SyncDeviceManage = "sync.device.manage";
    public const string ReceivedFiscalRead = "received_fiscal.read";
    public const string ReceivedFiscalImport = "received_fiscal.import";
    public const string ReceivedFiscalValidate = "received_fiscal.validate";
    public const string ReportsRead = "reports.read";
    public const string AuditRead = "audit.read";
    public const string AuditExport = "audit.export";
    public const string AlertsRead = "alerts.read";
    public const string AlertsManage = "alerts.manage";
    public const string OperationsRead = "operations.read";
    public const string IntegrationsRead = "integrations.read";
    public const string IntegrationsManage = "integrations.manage";
    public const string AccountingExport = "accounting.export";

    public const string OperationsMonitor = "operations.monitor";
    public const string OperationsTracesRead = "operations.traces.read";
    public const string OperationsMetricsRead = "operations.metrics.read";
    public const string OperationsQueuesRead = "operations.queues.read";
    public const string OperationsIntegrationsRead = "operations.integrations.read";
    public const string OperationsAlertsRead = "operations.alerts.read";
    public const string OperationsAlertsAcknowledge = "operations.alerts.acknowledge";
    public const string OperationsRetry = "operations.retry";
    public const string OperationsDiagnosticsExport = "operations.diagnostics.export";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        SecurityUsersRead, SecurityUsersManage, SecurityRolesRead, SecurityManageRoles,
        OrganizationRead, OrganizationManage,
        PartiesRead, PartiesManage, PartiesFiscalManage,
        CatalogRead, CatalogManage, PaymentsRead, PaymentsManage, DashboardRead,
        SalesRead, SalesCreate, SalesConfirm, SalesCancel,
        FiscalRead, FiscalCorrect, FiscalRegularizationManage, FiscalManageCae, FiscalManageContingency,
        FiscalReportRead, FiscalReportManage, FiscalConfigurationRead, FiscalConfigurationManage,
        InventoryRead, InventoryAdjust, InventoryTransfer,
        ProcurementRead, ProcurementManage, ProcurementApprove, ProcurementReceive,
        ReceivablesRead, ReceivablesAdjust, ReceivablesCollect,
        PayablesRead, PayablesAdjust, PayablesPay,
        CashRead, CashOpen, CashMove, CashClose, CashReconcile,
        SyncUse, SyncDeviceManage,
        ReceivedFiscalRead, ReceivedFiscalImport, ReceivedFiscalValidate,
        ReportsRead, AuditRead, AuditExport, AlertsRead, AlertsManage,
        OperationsRead, IntegrationsRead, IntegrationsManage, AccountingExport,
        OperationsMonitor, OperationsTracesRead, OperationsMetricsRead, OperationsQueuesRead,
        OperationsIntegrationsRead, OperationsAlertsRead, OperationsAlertsAcknowledge,
        OperationsRetry, OperationsDiagnosticsExport
    };

    public static bool IsKnown(string permission) => All.Contains(permission);
}
