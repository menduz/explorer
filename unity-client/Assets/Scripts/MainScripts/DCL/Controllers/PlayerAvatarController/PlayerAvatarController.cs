using System;
using System.Linq;
using DCL;
using UnityEngine;

public class PlayerAvatarController : MonoBehaviour
{
    public AvatarRenderer avatarRenderer;

    UserProfile userProfile => UserProfile.GetOwnUserProfile();
    bool repositioningWorld => DCLCharacterController.i.characterPosition.RepositionedWorldLastFrame();

    private void Awake()
    {
        avatarRenderer.SetVisibility(false);
    }

    private void OnEnable()
    {
        userProfile.OnUpdate += OnUserProfileOnUpdate;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!repositioningWorld && other.CompareTag("MainCamera"))
            avatarRenderer.SetVisibility(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!repositioningWorld && other.CompareTag("MainCamera"))
            avatarRenderer.SetVisibility(true);
    }

    private void OnUserProfileOnUpdate(UserProfile profile)
    {
        avatarRenderer.ApplyModel(profile.avatar, null, null);
    }

    [ContextMenu("All Male Wearables")]
    private void LoadUserWithAllMaleWearables()
    {
        var model = ModelWithAllWearables("dcl://base-avatars/BaseMale");
        avatarRenderer.ApplyModel(model, null, null);
    }

    [ContextMenu("All Female Wearables")]
    private void LoadUserWithAllFemaleWearables()
    {
        var model = ModelWithAllWearables("dcl://base-avatars/BaseFemale");
        avatarRenderer.ApplyModel(model, null, null);
    }

    private AvatarModel ModelWithAllWearables(string bodyShape)
    {

        AvatarModel model = new AvatarModel()
        {
            bodyShape = bodyShape,
            name = userProfile.avatar.name,
            eyeColor = userProfile.avatar.eyeColor,
            hairColor = userProfile.avatar.hairColor,
            skinColor = userProfile.avatar.skinColor,
        };
        model.wearables = CatalogController.wearableCatalog.dictionary.Where(x => x.Value.representations.Any(y => y.bodyShapes.Contains(model.bodyShape)) && x.Value.category != "body_shape").Select(x => x.Value.id).ToList();
        return model;
    }

    private void OnDisable()
    {
        userProfile.OnUpdate -= OnUserProfileOnUpdate;
    }
}
