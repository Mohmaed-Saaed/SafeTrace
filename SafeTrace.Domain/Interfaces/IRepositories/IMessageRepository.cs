using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IReposityory;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface IMessageRepository : IRepository<Message>
    {
    }
}
