using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum GameMode
{
    Kinect,
    Simulator
}

public class GameModeSwitcher : MonoBehaviour
{
    public GameMode mode = GameMode.Kinect;

    public GameObject simulator;              // объект Simulator
    public RigBuilder rigBuilder;             // Rig Builder на U_CharacterBack
    public AvatarController avatarController; // AvatarController на U_CharacterBack

    void Start()
    {
        ApplyMode();
    }

    public void SetModeKinect()
    {
        mode = GameMode.Kinect;
        ApplyMode();
    }

    public void SetModeSimulator()
    {
        mode = GameMode.Simulator;
        ApplyMode();
    }

    void ApplyMode()
    {
        bool useKinect = (mode == GameMode.Kinect);

        if (avatarController != null)
            avatarController.enabled = useKinect;

        if (rigBuilder != null)
            rigBuilder.enabled = !useKinect;

        if (simulator != null)
            simulator.SetActive(!useKinect);
    }
}
