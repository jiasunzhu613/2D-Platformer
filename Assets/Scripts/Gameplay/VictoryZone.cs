using UnityEngine;
using System;
using Player;

namespace Gameplay
{
    public class VictoryZone : MonoBehaviour
    {
        #region variables

        [SerializeField] private GameObject winMessage;
        // need textbox var here
        #endregion

        private void Awake()
        {
            throw new NotImplementedException();
        }

        private void start()
        {
            
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("ENTERED");
            other.gameObject.GetComponent<PlayerController>().can_control = false;
            other.gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            winMessage.SetActive(true);
        }
    }
}