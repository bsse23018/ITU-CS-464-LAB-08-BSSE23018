# Flight damage event

In [FlightSession.cs](../Assets/Lab07/Scripts/FlightSession.cs), the flight scene owns the remaining hearts and reports damage after updating state.

```csharp
public event Action<int, Vector2> Damaged;

// Inside Hit, after shield and invulnerability checks
Lives--;
Damaged?.Invoke(Lives, position);
```

In [FlightHudFeedback.cs](../Assets/Lab07/Scripts/FlightHudFeedback.cs), the HUD subscribes and unsubscribes with its Unity lifecycle.

```csharp
void OnEnable()
{
    session.Collected += OnCollected;
    session.Damaged += OnDamaged;
    session.ShieldBroken += OnShieldBroken;
}

void OnDisable()
{
    session.Collected -= OnCollected;
    session.Damaged -= OnDamaged;
    session.ShieldBroken -= OnShieldBroken;
}
```
