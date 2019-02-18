using DCL.Controllers;
using DCL.Helpers;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using DCL.Components;
using System.Collections.Generic;
using System.Linq;

public class EntityShapeAndOnClickTestController : MonoBehaviour
{
    IEnumerator Start()
    {
        var sceneController = FindObjectOfType<SceneController>();
        var scenesToLoad = (Resources.Load("TestJSON/SceneLoadingTest") as TextAsset).text;

        sceneController.UnloadAllScenes();
        sceneController.LoadParcelScenes(scenesToLoad);

        var scene = sceneController.loadedScenes["0,0"];

        TestHelpers.InstantiateEntityWithShape(scene, "1", DCL.Models.CLASS_ID.BOX_SHAPE, new Vector3(-3, 1, 0));
        TestHelpers.InstantiateEntityWithShape(scene, "2", DCL.Models.CLASS_ID.SPHERE_SHAPE, new Vector3(0, 1, 0));
        TestHelpers.InstantiateEntityWithShape(scene, "3", DCL.Models.CLASS_ID.PLANE_SHAPE, new Vector3(2, 1, 0));
        TestHelpers.InstantiateEntityWithShape(scene, "4", DCL.Models.CLASS_ID.CONE_SHAPE, new Vector3(4, 1, 0));
        TestHelpers.InstantiateEntityWithShape(scene, "5", DCL.Models.CLASS_ID.CYLINDER_SHAPE, new Vector3(6, 1, 0));
        // TestHelpers.InstantiateEntityWithShape(scene, "6", DCL.Models.CLASS_ID.GLTF_SHAPE, new Vector3(0, 1, 6), TestHelpers.GetTestsAssetsPath() + "/GLB/Trunk/Trunk.glb");
        // TestHelpers.InstantiateEntityWithShape(scene, "6", DCL.Models.CLASS_ID.GLTF_SHAPE, new Vector3(0, 1, 6), TestHelpers.GetTestsAssetsPath() + "/GLB/Lantern/Lantern.glb");
        TestHelpers.InstantiateEntityWithShape(scene, "6", DCL.Models.CLASS_ID.GLTF_SHAPE, new Vector3(0, 1, 6), TestHelpers.GetTestsAssetsPath() + "/GLTF/Trunk/Trunk.gltf");
        TestHelpers.InstantiateEntityWithShape(scene, "7", DCL.Models.CLASS_ID.OBJ_SHAPE, new Vector3(10, 1, 0), TestHelpers.GetTestsAssetsPath() + "/OBJs/teapot.obj");
        TestHelpers.InstantiateEntityWithShape(scene, "8", DCL.Models.CLASS_ID.GLTF_SHAPE, new Vector3(0, 1, 12), TestHelpers.GetTestsAssetsPath() + "/GLB/CesiumMan/CesiumMan.glb");

        Assert.IsNotNull(scene.entities["1"]);
        Assert.IsNotNull(scene.entities["2"]);
        Assert.IsNotNull(scene.entities["3"]);
        Assert.IsNotNull(scene.entities["4"]);
        Assert.IsNotNull(scene.entities["5"]);
        Assert.IsNotNull(scene.entities["6"]);
        Assert.IsNotNull(scene.entities["7"]);
        Assert.IsNotNull(scene.entities["8"]);

        TestHelpers.AddOnClickComponent(scene, "1", "event1");
        TestHelpers.AddOnClickComponent(scene, "2", "event1");
        TestHelpers.AddOnClickComponent(scene, "3", "event1");
        TestHelpers.AddOnClickComponent(scene, "4", "event1");
        TestHelpers.AddOnClickComponent(scene, "5", "event2");
        TestHelpers.AddOnClickComponent(scene, "6", "event3");
        TestHelpers.AddOnClickComponent(scene, "7", "event4");
        TestHelpers.AddOnClickComponent(scene, "8", "event5");

        var charController = FindObjectOfType<DCLCharacterController>();
        charController.SetPosition(JsonConvert.SerializeObject(new
        {
            x = 10,
            y = 0,
            z = 0
        }));

        // Assert.AreEqual(5, scene.disposableComponents.Where(a => a.Value is UUIDCallback).Count());

        //yield return new WaitForSeconds(5f);

        //scene.UpdateEntityComponent(JsonUtility.ToJson(new DCL.Models.UpdateEntityComponentMessage {
        //  entityId = "6",
        //  name = "shape",
        //  classId = (int)DCL.Models.CLASS_ID.GLTF_SHAPE,
        //  json = JsonConvert.SerializeObject(new {
        //    src = TestHelpers.GetTestsAssetsPath() + "/GLB/DamagedHelmet/DamagedHelmet.glb"
        //  })
        //}));

        string animJson = JsonConvert.SerializeObject(new DCLAnimator.Model
        {
            states = new DCLAnimator.Model.DCLAnimationState[] {
                new DCLAnimator.Model.DCLAnimationState {
                  name = "clip01",
                  clip = "animation:0",
                  playing = true,
                  looping = true,
                  weight = 1,
                  speed = 1
                }
            }
        });
        
        scene.EntityComponentCreate(JsonUtility.ToJson(new DCL.Models.EntityComponentCreateMessage
        {
            entityId = "8",
            name = "animation",
            classId = (int)DCL.Models.CLASS_ID_COMPONENT.ANIMATOR,
            json = animJson
        }));


        TestHelpers.CreateSceneEntity(scene, "9");
        scene.EntityComponentCreate(JsonUtility.ToJson(new DCL.Models.EntityComponentCreateMessage
        {
            entityId = "9",
            name = "text",
            classId = (int)DCL.Models.CLASS_ID_COMPONENT.TEXT_SHAPE,
            json = animJson
        }));

        var model = new TextShape.Model() { value = "Hello World!", width = 0.5f, height = 0.5f, hAlign = 0.5f, vAlign = 0.5f }; 
        TestHelpers.InstantiateEntityWithTextShape(scene, new Vector3(5, 5, 5), model);

        yield return null;
    }
}
