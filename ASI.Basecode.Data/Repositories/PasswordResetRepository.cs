using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;

namespace ASI.Basecode.Data.Repositories
{
    public class PasswordResetRepository : BaseRepository, IPasswordResetRepository
    {
        public PasswordResetRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        public IQueryable<PasswordResetToken> GetTokens()
        {
            return this.GetDbSet<PasswordResetToken>();
        }

        public void AddToken(PasswordResetToken model)
        {
            this.GetDbSet<PasswordResetToken>().Add(model);
            UnitOfWork.SaveChanges();
        }

        public void UpdateToken(PasswordResetToken model)
        {
            this.GetDbSet<PasswordResetToken>().Update(model);
            UnitOfWork.SaveChanges();
        }
    }
}