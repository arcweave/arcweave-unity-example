using UnityEngine;
using Arcweave;

public class ArcweaveVariableEvents : MonoBehaviour
{
    private ArcweavePlayer arcweavePlayer;
    private Animator animator;

    private void Start()
    {
        arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (arcweavePlayer?.aw?.Project == null || animator == null) return;

        CheckVariableConditions();
    }

    private void CheckVariableConditions()
    {
        try
        {
            // Esempio: controlla la vita e setta il parametro "IsWounded"
            var healthVar = arcweavePlayer.aw.Project.GetVariable("wanda_health");
            if (healthVar != null && healthVar.Type == typeof(int))
            {
                int health = (int)healthVar.Value;
                animator.SetBool("Healthy", health >= 40);
            }

        

            // Puoi aggiungere altre condizioni qui
            // var otherVar = arcweavePlayer.aw.Project.GetVariable("nome_variabile");
            // if (otherVar != null) { ... }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error checking variables: {e.Message}");
        }
    }
} 