using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class FixSnapshots : MonoBehaviour
{
    public CharacterPreviewController previewController;
    public TextAsset catalogJson;

    private void Awake()
    {
        var wearables = Newtonsoft.Json.JsonConvert.DeserializeObject<WearableItem[]>(catalogJson.text); // JsonUtility cannot deserialize jsons whose root is an array
        CatalogController.wearableCatalog.Clear();
        CatalogController.wearableCatalog.Add(wearables.Select(x => new KeyValuePair<string, WearableItem>(x.id, x)).ToArray());
    }

    private void Start()
    {
        var wearables = WearableLiterals.DefaultWearables.GetDefaultWearables(WearableLiterals.BodyShapes.FEMALE).ToList();
        var defaultModel =
            new UserProfileModel()
            {
                name = "prueba",
                userId = "prueba",
                avatar =  new AvatarModel()
                {
                    bodyShape = WearableLiterals.BodyShapes.FEMALE,
                    wearables = wearables,
                    skinColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)),
                    hairColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)),
                    eyeColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)),
                }
            };
        GetSnapshots(JsonUtility.ToJson(defaultModel));
    }

    public void GetSnapshots(string profileJson)
    {
        RetrieveSnapshots(JsonUtility.FromJson<UserProfileModel>(profileJson));
    }

    private void RetrieveSnapshots(UserProfileModel model)
    {
        previewController.UpdateModel(model.avatar, () =>
        {
            previewController.TakeSnapshots((face, body) =>
            {
                var directory = $"{Application.dataPath}/../{model.userId}/";
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(directory + "snapshot_face.png", face.EncodeToPNG());
                File.WriteAllBytes(directory + "snapshot_body.png", body.EncodeToPNG());
            });
        });
    }
}