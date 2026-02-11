using UnityEngine;
using JUTPSEditor.JUHeader;
namespace JUTPS.AI
{
    public class AIRandomDestination : MonoBehaviour
    {
        JUCharacterArtificialInteligenceBrain AICharacter;
        [JUHeader("AI Random Position Generation")]
        public Vector3 CenterPositionOffset;

        public float MinTime = 3, MaxTime = 10;
        public float Area = 100;
        private float currentMaxTime;
        private float currentTime;
        void Start()
        {
            AICharacter = GetComponent<JUCharacterArtificialInteligenceBrain>();
        }

        // Update is called once per frame
        void Update()
        {
            if (AICharacter == null) return;

            currentTime += Time.deltaTime;
            if (currentTime >= currentMaxTime)
            {
                GenerateNewRandomPosition();
                currentMaxTime = Random.Range(MinTime, MaxTime);
                currentTime = 0;
            }
        }
        public void GenerateNewRandomPosition()
        {
            Vector3 RandomPosition = Vector3.zero + CenterPositionOffset;
            RandomPosition.z += Random.Range(-Area, Area);
            RandomPosition.x += Random.Range(-Area, Area);
            AICharacter.Destination = RandomPosition;
        }
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(Vector3.zero + CenterPositionOffset, new Vector3(Area, 0, Area));
        }
    }
}