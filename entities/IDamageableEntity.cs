public interface IDamageableEntity
{
    public int Health { get; set; }
    public int MaxHealth { get; set; }

    public void Heal(int amount);
    public void TakeDamage(int damage);
    public void Kill();
}