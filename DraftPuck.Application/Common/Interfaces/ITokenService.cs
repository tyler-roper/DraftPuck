﻿using DraftPuck.Application.Features.Users;
using DraftPuck.Domain.Users;

namespace DraftPuck.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(UserEntity user);
    UserRefreshTokenEntity GenerateRefreshToken(string ipAddress);
}