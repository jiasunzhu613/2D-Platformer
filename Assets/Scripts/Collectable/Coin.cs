using System;
using Player;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Unity.VisualScripting;

namespace Collectables
{
    // maybe add sound later!
    public class Coin : MonoBehaviour
    {
        // #region Variables t
        private Collider2D collider_2d;
        private SpriteRenderer sr;
        // #endregion
        
        void Start()
        {
            collider_2d = GetComponent<Collider2D>();
            sr = GetComponent<SpriteRenderer>();
        }
        
        private void OnTriggerEnter2D(Collider2D other) // when collide with player, increment coin_counter by 1
        {
            var player = other.gameObject.GetComponent<PlayerController>();
            collider_2d.enabled = false;
            sr.enabled = false;
            player.coin_counter++;
        }
    }
}
