using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Common.Models;

public record UserDto(
    Guid Id,
    string UserName,
    string Gmail
);
    
