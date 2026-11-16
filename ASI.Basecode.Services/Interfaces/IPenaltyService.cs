using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IPenaltyService
    {
        // Custodian: the student paid the late fee, or handed over the replacement
        ServiceResult Resolve(int penaltyId, string custodianCode);

        // Admin: lift a restriction once nothing is owed
        BorrowerPenaltySummary GetSummary(int userDbId);
        ServiceResult LiftRestriction(int userDbId, string adminCode);
    }
}