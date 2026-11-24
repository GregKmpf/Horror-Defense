using UnityEngine;

// How the in-game UI writes numbers, times and turret stats, in Brazilian Portuguese
public static class UIFormat
{
    // 3:05
    public static string Time(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    // 1,5 instead of 1.5, and no ",0" on whole numbers
    public static string Number(float value)
    {
        return value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
    }

    public static string Money(int value)
    {
        return "$ " + value;
    }

    public static string Priority(TargetPriority priority)
    {
        switch (priority)
        {
            case TargetPriority.Last:
                return "Último";
            case TargetPriority.Closest:
                return "Mais perto";
            case TargetPriority.Strongest:
                return "Mais forte";
            default:
                return "Primeiro";
        }
    }

    // Damage, range and rate of fire in one line, plus what makes the turret different
    public static string Stats(TurretDefinition turret)
    {
        string damage = $"Dano {Number(turret.damage)}";
        if (turret.maxTargets == 0)
        {
            damage += " em todos no alcance";
        }
        else if (turret.splashRadius > 0f)
        {
            damage += $" em área ({Number(turret.splashRadius)} m)";
        }

        float shotsPerSecond = turret.fireInterval > 0f ? 1f / turret.fireInterval : 0f;
        string rate = Mathf.Approximately(shotsPerSecond, 1f) ? "1 tiro/s"
            : shotsPerSecond > 1f ? $"{Number(shotsPerSecond)} tiros/s"
            : $"1 tiro a cada {Number(turret.fireInterval)} s";

        return $"{damage}  ·  Alcance {Number(turret.range)} m  ·  {rate}";
    }
}
