using UnityEngine;
using System.Collections.Generic;
public abstract class Trait : ScriptableObject
{
    [Header("Trait Identifier")]
    public CommanderTrait identifier;
    public abstract void ProcessEventOutcome(bool wasSuccess, List<ParameterChange> outcomeChanges);
}