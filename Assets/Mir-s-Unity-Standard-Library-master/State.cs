//to avoid making your code dirty with a bunch lines of if else(like a famous game developer has name start with 'Y') State Machine is recommended 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class State
{
    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    }

    public enum Name
    {
        ATTACK, PATROL, CHASE
    }

    public Name stateName;
    public State nextState;
    protected EVENT stage = EVENT.ENTER;

    protected virtual void Enter()
    {
        stage = EVENT.UPDATE;
        // Debug.Log(stateName);
    }

    protected virtual void Update()
    {
        stage = EVENT.UPDATE;
    }
    protected virtual void Exit()
    {
        stage = EVENT.EXIT;
    }
    public State Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }
        return this;
    }

}
