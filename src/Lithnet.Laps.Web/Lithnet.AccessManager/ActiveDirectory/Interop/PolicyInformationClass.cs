using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    enum PolicyInformationClass
    {
        PolicyAuditLogInformation = 1,
        PolicyAuditEventsInformation,
        PolicyPrimaryDomainInformation,
        PolicyPdAccountInformation,
        PolicyAccountDomainInformation,
        PolicyLsaServerRoleInformation,
        PolicyReplicaSourceInformation,
        PolicyDefaultQuotaInformation,
        PolicyModificationInformation,
        PolicyAuditFullSetInformation,
        PolicyAuditFullQueryInformation,
        PolicyDnsDomainInformation
    }
}
