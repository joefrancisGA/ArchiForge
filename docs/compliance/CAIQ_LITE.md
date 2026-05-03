> **Scope:** Consensus Assessments Initiative Questionnaire (CAIQ) Lite for ArchLucid.

# CAIQ Lite - ArchLucid

This document answers standard enterprise security questions based on the ArchLucid architecture.

**1. Does the system provide tenant isolation?**
Yes. ArchLucid supports multi-tenancy at the data layer using row-level security (RLS) and tenant-specific identifiers in all core tables.

**2. How is data backed up?**
Data is stored in SQL Server. Customers are responsible for configuring SQL Server backups (e.g., Azure SQL Database automated backups) according to their RPO and RTO requirements.

**3. How are API keys managed?**
API keys are configured via the `Authentication:ApiKey` section in `appsettings.json` or environment variables. They are not stored in plaintext in the database. The system defaults to a fail-closed state (`Enabled=false`).

**4. Is data encrypted at rest?**
Yes, when deployed on Azure SQL Database, Transparent Data Encryption (TDE) is enabled by default.

**5. Is data encrypted in transit?**
Yes, all external communication requires HTTPS (TLS 1.2+).

**6. Does the system support role-based access control (RBAC)?**
Yes. ArchLucid maps identity provider roles to `Admin`, `Operator`, and `Reader` policies.

**7. How are AI models accessed?**
ArchLucid integrates with Azure OpenAI. Communication is secured via HTTPS and authenticated using Microsoft Entra ID or API keys. No customer data is used to train the base models.

**8. Is there an audit trail for actions?**
Yes. All runs, results, and commits are persisted in SQL Server, providing a historical record of architecture decisions.

**9. Can the system be deployed in a private network?**
Yes. ArchLucid is containerized and can be deployed in private VNets, AKS, or Azure Container Apps without public inbound access (except via a WAF/Front Door).

**10. How are vulnerabilities managed?**
The repository uses automated secret scanning and dependency scanning in CI to detect and remediate vulnerabilities before deployment.