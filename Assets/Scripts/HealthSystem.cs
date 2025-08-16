using UnityEngine;

[System.Serializable]
public class HealthSystem
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => (currentHealth / maxHealth) * 100f;
    public bool IsDead => currentHealth <= 0;
    
    public System.Action<float> OnHealthChanged;
    public System.Action OnDeath;
    
    public HealthSystem(float maxHealth)
    {
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0 && oldHealth > 0)
        {
            OnDeath?.Invoke();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);
    }
}
