using CounterStrikeSharp.API.Modules.Utils;

namespace IksAdmin_FunCommands.Extensions;

public static class PositionExtensions
{
    public static Vector Clone(this Vector vector)
    {
        return new Vector(vector.X, vector.Y, vector.Z);
    }
    public static QAngle Clone(this QAngle qAngle)
    {
        return new QAngle(qAngle.X, qAngle.Y, qAngle.Z);
    }
}