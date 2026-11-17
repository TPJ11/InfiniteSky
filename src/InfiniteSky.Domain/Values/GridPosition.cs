namespace InfiniteSky.Domain.Values;

/// <summary>
/// A zero-based inventory or storage grid position.
/// </summary>
public readonly record struct GridPosition(uint Column, uint Row);
