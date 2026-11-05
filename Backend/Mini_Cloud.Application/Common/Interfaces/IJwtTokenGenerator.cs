using System;
using System.Collections.Generic;
using System.Text;
using Mini_Cloud.Domain.Entities;

namespace Mini_Cloud.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
