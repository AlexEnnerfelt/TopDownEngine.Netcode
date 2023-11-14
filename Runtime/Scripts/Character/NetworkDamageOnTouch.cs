using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkDamageOnTouch : DamageOnTouch {
		protected override void OnCollideWithDamageable(Health health) {
			_collidingHealth = health;

			if (health.CanTakeDamageThisFrame()) {
				// if what we're colliding with is a TopDownController, we apply a knockback force
				_colliderTopDownController = health.gameObject.MMGetComponentNoAlloc<TopDownController>();

				HitDamageableFeedback?.PlayFeedbacks(this.transform.position);

				// we apply the damage to the thing we've collided with
				var randomDamage =
					Random.Range(MinDamageCaused, Mathf.Max(MaxDamageCaused, MinDamageCaused));

				ApplyKnockback(randomDamage, TypedDamages);

				DetermineDamageDirection();

				if (RepeatDamageOverTime) {
					_colliderHealth.DamageOverTime(randomDamage, gameObject, InvincibilityDuration, InvincibilityDuration, _damageDirection, TypedDamages, AmountOfRepeats, DurationBetweenRepeats, DamageOverTimeInterruptible, RepeatedDamageType);
					//TODO implement damage over time for netcode here
				} else {
					_colliderHealth.Damage(randomDamage, gameObject, InvincibilityDuration, InvincibilityDuration, _damageDirection, TypedDamages);
				}
			}

			// we apply self damage
			if (DamageTakenEveryTime + DamageTakenDamageable > 0 && !_colliderHealth.PreventTakeSelfDamage) {
				SelfDamage(DamageTakenEveryTime + DamageTakenDamageable);
			}
		}
	}
}