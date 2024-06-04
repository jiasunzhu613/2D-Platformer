using UnityEngine;
using System;

namespace Gameplay
{
    public class ParallaxLayer : MonoBehaviour
    {
        private Vector3 initialPosition;
        public float parallaxEffect;
        private Transform _camera;
        private Vector3 prevCameraPosition;
        private Vector3 changeInCameraPosition;
        private float length;
        public GameObject sprite;
        private void Awake()
        {
            initialPosition = transform.position;
            _camera = Camera.main.transform;
            prevCameraPosition = _camera.position;
            length = sprite.GetComponent<SpriteRenderer>().bounds.size.x;
        }

        private void LateUpdate()
        {
            float temp = _camera.position.x * (1 - parallaxEffect);
            changeInCameraPosition = _camera.position - prevCameraPosition;
            transform.position = initialPosition + changeInCameraPosition * parallaxEffect;

            if (temp > initialPosition.x + length) initialPosition.x += length;
            if (temp < initialPosition.x - length) initialPosition.x -= length;
        }
    }
}