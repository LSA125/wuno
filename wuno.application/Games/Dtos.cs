using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public record NewGameRequest(int PlayerCount = 2, int TargetWins = 2);
    public record NewGameResponse(Guid GameId, Guid TurnId, int NextSeat, int PlayerCount, int TargetWins);
    public record SubmitWordRequest(int Seat, string Word);
    public record SubmitWordResponse(bool Ok, string? Reason);
}
