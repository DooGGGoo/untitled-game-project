using Godot;

[GlobalClass]
public partial class BreakableProp : PhysicsProp, IDamageableEntity
{
    [Export] public int MaxHealth { get; set; }
    public int Health {get; set; }

    public override void _Ready()
    {
        base._Ready();

        Health = MaxHealth;
    }

    public void Heal(int amount)
    {
        if (MaxHealth <= 0) return;

        Health += amount;
    }

    public void TakeDamage(int damage)
    {
        if (MaxHealth <= 0) return;

        Health -= damage;
        if (Health <= 0)
            Kill();
    }

    public void Kill()
    {
        GD.Print("Bye");
        QueueFree();
    }

    public override void OnImpact(Node node)
    {
        base.OnImpact(node);

        int damage = Mathf.FloorToInt(Mathf.Abs(LinearVelocity.Length()) / Mass) * 2;

        GD.Print(damage + " | " + Health);
        TakeDamage(damage);
    }
}