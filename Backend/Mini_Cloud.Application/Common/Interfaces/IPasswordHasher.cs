using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string providedPassword, string hashedPassword);
    }
}
