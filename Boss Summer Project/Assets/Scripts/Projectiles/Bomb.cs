using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : Projectile
{
    [SerializeField] private float bombRange;
    [SerializeField] private GameObject explosion;

    private GameObject bombTarget;

    void Start()
    {

    }
    protected override void Update()
    {
        base.Update();
    }
    protected override void DoCollision(Collider2D col) {
        GameObject explosionParticles = Instantiate(explosion, transform.position, Quaternion.identity);

        Destroy(explosionParticles, 2f);

        bombTarget = col.gameObject;
        //If the bomb directly hits a damageable entity, it applies the full damage
        Damageable directHitScript = bombTarget.GetComponent<Damageable>();
        if (directHitScript != null) {
            directHitScript.TakeDamage(damage);
        }

        //The bomb also applies damage to all entities within its 1.5 unit range depending on the distance
        Collider2D[] allEntitiesInRange = Physics2D.OverlapCircleAll(transform.position, bombRange);

        foreach (Collider2D collider in allEntitiesInRange) {

            Damageable targetScript = collider.gameObject.GetComponent<Damageable>();
            if(targetScript != null && targetScript != directHitScript) {
                float distanceBetweenBombAndTarget = (collider.gameObject.transform.position - transform.position).magnitude;
                float damagePercentage = (bombRange-distanceBetweenBombAndTarget)/bombRange;

                if (damagePercentage < 0)
                    damagePercentage = 0;
                    
                targetScript.TakeDamage(damagePercentage * damage);
            }
        }

        Destroy(gameObject);
    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        base.OnCollisionEnter2D(col);
        GameObject explosionParticles = Instantiate(explosion, transform.position, Quaternion.identity);

        Destroy(explosionParticles, 2f);
    }
}
