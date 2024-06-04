using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Gameplay
{
    public class DeathZone : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("ENTERED");
            other.gameObject.GetComponent<PlayerController>().EnterDeadState();
            // player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
    }

}
