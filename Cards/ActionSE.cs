using UnityEngine;

public enum ActionSE
{
    MoveSlime,  //
    MoveMimic,  //
    MovePlant,  //

    AttackSlime,  //
    AttackMimic,  //
    AttackPlant  //
}

[System.Serializable]
public class ActionSEData
{
    public ActionSE actionSE;
    public AudioClip clip;
}
