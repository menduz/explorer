using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SSAOToggler : MonoBehaviour
{
    public PostProcessProfile profile;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            profile.GetSetting<SSAOSimple>().active = !profile.GetSetting<SSAOSimple>().active;
        }
    }
}
