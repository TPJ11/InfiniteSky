using InfiniteSky.Domain.Items;

namespace InfiniteSky.Domain.Accounts;

/// <summary>
/// Per-server account data stored by the original CharacterIndividuals object.
/// </summary>
public sealed class AccountServerData
{
    /// <summary>
    /// Gets the ordered 56-slot account bank; absent slots are represented by null.
    /// </summary>
    public IList<InventoryItem?> Bank { get; } = [];

    /// <summary>
    /// Gets or sets the silver stored in the bank.
    /// </summary>
    public long BankSilver { get; set; }
}
