namespace BlazorKompovNet.Models;

public static class ComputerDisplay
{
    public static string GetTitle(Computer computer)
    {
        if (!string.IsNullOrWhiteSpace(computer.Name))
        {
            return computer.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(computer.Number))
        {
            return $"ПК {computer.Number.Trim()}";
        }

        return $"ПК #{computer.Id}";
    }

    public static string GetSubtitle(Computer computer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(computer.Number) &&
            !string.Equals(computer.Number.Trim(), computer.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"№ {computer.Number.Trim()}");
        }

        parts.Add($"ID {computer.Id}");
        return string.Join(" · ", parts);
    }

    public static string GetSpecsLine(Computer computer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(computer.Processor))
        {
            parts.Add(computer.Processor.Trim());
        }
        else if (computer.Zone is { CPU: { Length: > 0 } cpu })
        {
            parts.Add(cpu.Trim());
        }

        if (!string.IsNullOrWhiteSpace(computer.GraphicsCard))
        {
            parts.Add(computer.GraphicsCard.Trim());
        }
        else if (computer.Zone is { GPU: { Length: > 0 } gpu })
        {
            parts.Add(gpu.Trim());
        }

        if (computer.RamGb > 0)
        {
            parts.Add($"{computer.RamGb} ГБ RAM");
        }
        else if (computer.Zone is { RAM: { Length: > 0 } ram })
        {
            parts.Add(ram.Trim());
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : "Характеристики не указаны";
    }

    public static string GetMonitorLine(Computer computer)
    {
        if (!string.IsNullOrWhiteSpace(computer.Monitor))
        {
            return computer.Monitor.Trim();
        }

        if (computer.Zone is not null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(computer.Zone.MonitorSize))
            {
                parts.Add(computer.Zone.MonitorSize.Trim());
            }

            if (!string.IsNullOrWhiteSpace(computer.Zone.MonitorResolution))
            {
                parts.Add(computer.Zone.MonitorResolution.Trim());
            }

            if (!string.IsNullOrWhiteSpace(computer.Zone.MonitorHz))
            {
                parts.Add($"{computer.Zone.MonitorHz.Trim()} Гц");
            }

            if (parts.Count > 0)
            {
                return string.Join(" · ", parts);
            }
        }

        return "Монитор не указан";
    }

    public static string GetListLabel(Computer computer)
    {
        return $"{GetTitle(computer)} · {computer.Zone?.Name ?? "—"} · {GetStatusName(computer)}";
    }

    public static string GetStatusName(Computer computer)
    {
        return computer.Status?.Name ?? "Неизвестно";
    }

    public static string GetTitleOrDefault(Computer? computer, string fallback = "—")
    {
        return computer is null ? fallback : GetTitle(computer);
    }
}
