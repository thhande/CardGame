using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardData : ScriptableObject
{
    [field: SerializeField] public string CardName { get; private set; }
    [field: SerializeField] public string CardDescription { get; private set; }
    [field: SerializeField] public Sprite Artwork { get; private set; }
    protected CardType type;
    public virtual CardType Type => type;

    public abstract CardDefinition ToDefinition();
}
