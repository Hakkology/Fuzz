namespace Fuzz.Domain.Entities;

[Flags]
public enum AiCapabilities
{
    Text = 1,
    Visual = 2,
    Sound = 4,
    Voice = 8
}
