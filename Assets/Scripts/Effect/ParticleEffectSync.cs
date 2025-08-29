using Photon.Pun;
using UnityEngine;

public class ParticleEffectSync : MonoBehaviourPun
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    [PunRPC]
    public void RPC_SetupAndPlay(float duration, ParticleSystemStopAction stopAction)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.duration = duration;
            main.stopAction = stopAction;
            ps.Play();
        }
    }

    [PunRPC]
    public void RPC_SetupParentAndPlay(Transform target, float duration, ParticleSystemStopAction stopAction)
    {
        if (ps != null)
        {
            transform.SetParent(target);
            var main = ps.main;
            main.duration = duration;
            main.stopAction = stopAction;
            ps.Play();
        }
    }

}
