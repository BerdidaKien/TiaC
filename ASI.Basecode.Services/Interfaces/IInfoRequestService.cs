using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IInfoRequestService
    {
        // ---- Student / custodian side ----
        InfoRequestFormViewModel GetForm(string userCode);
        ServiceResult Submit(string userCode, SubmitInfoRequest model, IList<StoredProof> proofs);
        ServiceResult Cancel(int requestId, string userCode);

        // ---- Admin side ----
        AdminInfoRequestsViewModel GetAdminPage(string status, string search, int page);
        InfoRequestRow GetRequestRow(int id);
        ServiceResult Approve(int requestId, IDictionary<string, string> values, string note, string adminCode);
        ServiceResult Reject(int requestId, string reason, string adminCode);
        InfoChangeRequestFile GetFile(int fileId);
        int PendingCount();
    }
}