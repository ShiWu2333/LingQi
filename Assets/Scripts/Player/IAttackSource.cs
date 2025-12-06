using System;

public interface IAttackSource
{
    event Action<AttackData> OnAttackStarted;
    event Action<AttackData> OnAttackEnded;
}
