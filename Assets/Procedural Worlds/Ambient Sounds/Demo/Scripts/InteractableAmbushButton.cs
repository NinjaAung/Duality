using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientSounds.Demo
{

   

    public class InteractableAmbushButton : InteractableSign {

        public GameObject enemyPrefab;
        public Transform enemyTargetTransform1, enemyTargetTransform2, enemyTargetTransform3;

        private GameObject enemyReference1, enemyReference2, enemyReference3;

        override public void Interact()
        {
            InstantiateEnemy(ref enemyReference1, enemyTargetTransform1);
            InstantiateEnemy(ref enemyReference2, enemyTargetTransform2);
            InstantiateEnemy(ref enemyReference3, enemyTargetTransform3);
            base.Interact();
        }

        private void Update()
        {
            if (enemyReference1 == null &&
                enemyReference2 == null &&
                enemyReference3 == null)
            {
                AmbienceManager.DeactivateEvent("Combat");
            }
            else
            {
                AmbienceManager.ActivateEvent("Combat");
            }
        }

        private void InstantiateEnemy(ref GameObject reference, Transform transform)
        {
            if (reference == null)
            {
                reference = Instantiate(enemyPrefab, transform.position, transform.rotation);
            }
        }



    }
}
