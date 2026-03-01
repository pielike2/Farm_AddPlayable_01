using Playable.Models;
using UnityEngine;

namespace Base
{
    public class SceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            var sceneVars = GetComponent<SceneBasicVariables>();
            ModelsContainer.SceneVariables = sceneVars;
        }
    }
}