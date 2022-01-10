using UnityEngine;
using UnityEditor;
using System.Xml;
using System.IO;
using UnityEngine.UI;

public class PrefabParseToXMl : Editor
{
    //将所有游戏场景导出为XML格式
    [MenuItem("Assets/PrefabParseToXMl", false, 1)]
    [MenuItem("GameObject/PrefabParseToXMl", false, 1)]
    [MenuItem("Window/PrefabParseToXMl")]
    private static void ExportXML()
    {
        // string filepath = Application.dataPath + @"/../UnityPrefabParseXml/ParseXml.xml";
        string filePath = EditorUtility.SaveFilePanel("Save Resource", "", "PrefabParseToXMl", "xml");
        if (!File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        if (filePath.Length != 0)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("e:Skin");
            UnityEngine.Object[] selectedPrefabs = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);
            foreach (var selectPrefab in selectedPrefabs)
            {
                //当关卡启用
                if (selectPrefab != null)
                {
                    root.SetAttribute("class", selectPrefab.name + "Skin");
                    var selectGameObj = selectPrefab as GameObject;
                    ParseChildren(xmlDoc, root, selectGameObj.transform);
                    root.SetAttribute("width", Screen.width.ToString());
                    root.SetAttribute("height", Screen.height.ToString());
                    root.SetAttribute("xmlns:e", "http://ns.egret.com/eui");
                    xmlDoc.AppendChild(root);
                    xmlDoc.Save(filePath);
                    #region 注释掉的之前版本代码,留着做以后参考可能有用
                    // foreach (GameObject obj in selectPrefab)
                    // {
                    //     //判断是否是prefab
                    //     // if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab) 
                    //     // {
                    //     //从场景的预制体获取本地的预制体对象
                    //     //UnityEngine.Object prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    //     //if (prefabObject != null)
                    //     //{
                    //     XmlElement gameObject = xmlDoc.CreateElement("Group");
                    //     gameObject.SetAttribute("id", obj.name);
                    //     gameObject.SetAttribute("visible", obj.activeSelf.ToString().ToLower());

                    //     var components = obj.GetComponents<Component>();
                    //     UnityComponentParseToH5(xmlDoc, gameObject, components);

                    //     root.SetAttribute("width", Screen.width.ToString());
                    //     root.SetAttribute("height", Screen.height.ToString());
                    //     root.SetAttribute("xmlns:e", "http://ns.egret.com/eui");
                    //     root.AppendChild(gameObject);
                    //     xmlDoc.AppendChild(root);
                    //     xmlDoc.Save(filePath);
                    //     //}
                    //     // }
                    // }
                    #endregion
                }
            }
        }
        //刷新Project视图， 不然需要手动刷新哦
        AssetDatabase.Refresh();
        Debug.Log($"已经解析完成!");
    }

    private static void ParseChildren(XmlDocument xmlDoc, XmlElement currentElement, Transform currentGObjTsf)
    {
        UnityComponentParseToH5(xmlDoc, currentElement, currentGObjTsf.GetComponents<Component>());
        for (int i = 0; i < currentGObjTsf.childCount; i++)
        {
            var child = currentGObjTsf.GetChild(i);
            var childGameObj = xmlDoc.CreateElement("e:Group");
            childGameObj.SetAttribute("id", child.name);
            childGameObj.SetAttribute("visible", child.gameObject.activeSelf.ToString().ToLower());
            // Debug.Log($"childName:{child.name},childCount:{child.childCount}");
            if (child.childCount > 0)
            {
                ParseChildren(xmlDoc, childGameObj, child);
            }
            else
            {
                var childComponents = child.GetComponents<Component>();
                UnityComponentParseToH5(xmlDoc, childGameObj, childComponents);
            }
            currentElement.AppendChild(childGameObj);
        }
    }


    private static void UnityComponentParseToH5(XmlDocument xmlDoc, XmlElement gameObject, Component[] components)
    {
        foreach (var component in components)
        {
            if (component != null)
            {
                XmlElement componentProps;
                //求出最后一个.字符的位置
                var i = component.GetType().ToString().LastIndexOf('.');
                //从指定位置截取字符串
                var componentName = component.GetType().ToString().Substring(i + 1);
                switch ($"{componentName}")
                {
                    case nameof(RectTransform):
                        var rectTransform = component as RectTransform;
                        componentProps = xmlDoc.CreateElement(nameof(RectTransform));
                        gameObject.SetAttribute("x", (int)rectTransform.localPosition.x + "");
                        gameObject.SetAttribute("y", (int)rectTransform.localPosition.y + "");
                        componentProps.SetAttribute("scaleX", (int)rectTransform.localScale.x + "");
                        componentProps.SetAttribute("scaleY", (int)rectTransform.localScale.y + "");
                        componentProps.SetAttribute("rotation", (int)rectTransform.localEulerAngles.z + "");
                        componentProps.SetAttribute("width", (int)rectTransform.rect.width + "");
                        componentProps.SetAttribute("height", (int)rectTransform.rect.height + "");
                        componentProps.SetAttribute("anchorOffsetX", (int)rectTransform.pivot.x + "");
                        componentProps.SetAttribute("anchorOffsetY", (int)rectTransform.pivot.y + "");
                        // componentProps.SetAttribute("anchorMin", rectTransform.anchorMin.ToString() + "");
                        // componentProps.SetAttribute("anchorMax", rectTransform.anchorMax.ToString() + "");
                        if (rectTransform.anchorMin == new Vector2(0.5f, 0.5f) && rectTransform.anchorMax == new Vector2(0.5f, 0.5f))
                        {
                            componentProps.SetAttribute("horizontalCenter", 0 + "");
                            componentProps.SetAttribute("verticalCenter", 0 + "");
                        }
                        break;
                    case nameof(Canvas):
                        var canvas = component as Canvas;
                        componentProps = xmlDoc.CreateElement("e:Canvas");
                        componentProps.SetAttribute("renderMode", canvas.renderMode.ToString());
                        switch (canvas.renderMode)
                        {
                            case RenderMode.ScreenSpaceOverlay:
                                componentProps.SetAttribute("pixelPerfect", canvas.pixelPerfect.ToString());
                                componentProps.SetAttribute("sortOrder", canvas.sortingOrder.ToString());
                                componentProps.SetAttribute("targetDisplay", canvas.targetDisplay.ToString());
                                break;
                            case RenderMode.ScreenSpaceCamera:
                                componentProps.SetAttribute("pixelPerfect", canvas.pixelPerfect.ToString());
                                componentProps.SetAttribute("renderCamera", canvas.worldCamera?.name);
                                componentProps.SetAttribute("orderInLayer", canvas.sortingOrder.ToString());
                                break;
                            case RenderMode.WorldSpace:
                                componentProps.SetAttribute("eventCamera", canvas.worldCamera?.ToString());
                                componentProps.SetAttribute("sortingLayer", canvas.sortingLayerName.ToString());
                                componentProps.SetAttribute("orderInLayer", canvas.sortingOrder.ToString());
                                break;
                        }
                        componentProps.SetAttribute("additionalShaderChannels", canvas.additionalShaderChannels.ToString());
                        break;
                    case nameof(CanvasScaler):
                        var canvasScaler = component as CanvasScaler;
                        componentProps = xmlDoc.CreateElement("e:CanvasScaler");
                        componentProps.SetAttribute("uiScaleMode", canvasScaler.uiScaleMode.ToString());
                        switch (canvasScaler.uiScaleMode)
                        {
                            case CanvasScaler.ScaleMode.ConstantPixelSize:
                                componentProps.SetAttribute("scaleFactor", canvasScaler.scaleFactor.ToString());
                                break;
                            case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                componentProps.SetAttribute("referenceResolution", canvasScaler.referenceResolution.ToString());
                                componentProps.SetAttribute("screenMatchMode", canvasScaler.screenMatchMode.ToString());
                                componentProps.SetAttribute("match", canvasScaler.matchWidthOrHeight.ToString());
                                break;
                            case CanvasScaler.ScaleMode.ConstantPhysicalSize:
                                componentProps.SetAttribute("physicalUnit", canvasScaler.physicalUnit.ToString());
                                componentProps.SetAttribute("fallbackScreenDPI", canvasScaler.fallbackScreenDPI.ToString());
                                componentProps.SetAttribute("defaultSpriteDPI", canvasScaler.defaultSpriteDPI.ToString());
                                break;
                        }
                        componentProps.SetAttribute("referencePixelsPerUnit", canvasScaler.referencePixelsPerUnit.ToString());
                        break;
                    case nameof(GraphicRaycaster):
                        var graphicRaycaster = component as GraphicRaycaster;
                        componentProps = xmlDoc.CreateElement("e:GraphicRaycaster");
                        componentProps.SetAttribute("ignoreReversedGraphics", graphicRaycaster.ignoreReversedGraphics.ToString());
                        componentProps.SetAttribute("blockingObjects", graphicRaycaster.blockingObjects.ToString());
                        break;
                    case nameof(Image):
                        var image = component as Image;
                        componentProps = xmlDoc.CreateElement("e:Image");
                        var spritePath = AssetDatabase.GetAssetPath(image.sprite.GetInstanceID());
                        if (spritePath != "Resources/unity_builtin_extra" && spritePath.Length != 0)
                        {
                            var spriteSplitPaths = spritePath.Split('/');
                            var spriteName = spriteSplitPaths[spriteSplitPaths.Length - 1].Split('.');
                            string spriteFolderName = null;
                            spriteFolderName = spriteSplitPaths[spriteSplitPaths.Length - 2];
                            var newSpriteName = spriteFolderName[0].ToString().ToLower() + spriteFolderName.Substring(1) + "_" + spriteName[0] + "_" + spriteName[1];
                            componentProps.SetAttribute("source", newSpriteName);
                            componentProps.SetAttribute("color", image.color.ToString());
                            componentProps.SetAttribute("material", image.material?.ToString());
                            componentProps.SetAttribute("raycastTarget", image.raycastTarget.ToString());
                            componentProps.SetAttribute("imageType", image.type.ToString());
                            switch (image.type)
                            {
                                case Image.Type.Simple:
                                    componentProps.SetAttribute("useSpriteMesh", image.useSpriteMesh.ToString());
                                    componentProps.SetAttribute("preserveAspect", image.preserveAspect.ToString());
                                    break;
                                case Image.Type.Sliced:
                                case Image.Type.Tiled:
                                    componentProps.SetAttribute("fillCenter", image.fillCenter.ToString());
                                    break;
                                case Image.Type.Filled:
                                    componentProps.SetAttribute("fillMethod", image.fillMethod.ToString());
                                    componentProps.SetAttribute("fillOrigin", image.fillOrigin.ToString());
                                    componentProps.SetAttribute("fillAmout", image.fillAmount.ToString());
                                    componentProps.SetAttribute("clockwise", image.fillClockwise.ToString());
                                    componentProps.SetAttribute("preservbeAspet", image.preserveAspect.ToString());
                                    break;
                            }
                        }
                        else
                        {
                            componentProps.SetAttribute("source", "white_png");
                        }
                        break;
                    case nameof(Button):
                        var button = component as Button;
                        componentProps = xmlDoc.CreateElement("e:Button");
                        componentProps.SetAttribute("interactable", button.interactable.ToString());
                        componentProps.SetAttribute("transition", button.transition.ToString());
                        switch (button.transition)
                        {
                            case Button.Transition.None:
                                break;
                            case Button.Transition.Animation:
                                break;
                            case Button.Transition.ColorTint:
                                componentProps.SetAttribute("targetGraphic", button.targetGraphic.name);
                                break;
                            case Button.Transition.SpriteSwap:
                                break;
                        }
                        componentProps.SetAttribute("navigationMode", button.navigation.mode.ToString());
                        switch (button.navigation.mode)
                        {
                            case Navigation.Mode.None:
                                break;
                            case Navigation.Mode.Automatic:
                                break;
                            case Navigation.Mode.Explicit:
                                break;
                            case Navigation.Mode.Horizontal:
                                break;
                            case Navigation.Mode.Vertical:
                                break;
                        }
                        if (button.onClick.GetPersistentEventCount() > 0)
                        {
                            for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                            {
                                componentProps.SetAttribute($"onClickEventName+{index + 1}", button.onClick.GetPersistentMethodName(index));
                            }
                        }
                        break;
                    case nameof(InputField):
                        var inputField = component as InputField;
                        componentProps = xmlDoc.CreateElement("e:EditableText");
                        break;
                    case nameof(Text):
                        var text = component as Text;
                        componentProps = xmlDoc.CreateElement("e:Label");
                        componentProps.SetAttribute("text", text.text);
                        componentProps.SetAttribute("fontFamily", text.font.ToString());
                        componentProps.SetAttribute("textColor", text.color.ToString());
                        componentProps.SetAttribute("size", text.fontSize.ToString());
                        componentProps.SetAttribute("normal", (text.fontStyle == FontStyle.Normal).ToString());
                        componentProps.SetAttribute("bold", (text.fontStyle == FontStyle.Bold).ToString());
                        componentProps.SetAttribute("italic", (text.fontStyle == FontStyle.Italic).ToString());
                        if (text.fontStyle == FontStyle.BoldAndItalic)
                        {
                            componentProps.SetAttribute("bold", "true");
                            componentProps.SetAttribute("italic", "true");
                        }
                        if (text.alignment.ToString() == "MiddleCenter")
                        {
                            componentProps.SetAttribute("verticalAlign", "center");
                            componentProps.SetAttribute("TextAlign", "center");
                        }
                        componentProps.SetAttribute("lineSpacing", (int)text.lineSpacing + "");
                        break;
                    case nameof(Slider):
                        var slider = component as Slider;
                        componentProps = xmlDoc.CreateElement("e:ProgressBar");
                        break;
                    case nameof(Scrollbar):
                        var scrollbar = component as Scrollbar;
                        componentProps = xmlDoc.CreateElement("e:Scroller");
                        break;
                    case nameof(ScrollRect):
                        var scrollRect = component as ScrollRect;
                        componentProps = xmlDoc.CreateElement("e:List");
                        break;
                    //我们项目自己写的一个脚本OutlineEx,测试用不了，必须在项目中才能开启
                    //case "OutlineEx":
                    // var outLine = component as OutlineEx;
                    //componentProps = xmlDoc.CreateElement("e:OutLine");
                    // componentProps.SetAttribute("stroke", outLine.OutlineWidth);
                    // componentProps.SetAttribute("strokeColor", outLine.OutlineColor);
                    //break;
                    default:
                        //componentProps = xmlDoc.CreateElement($"{componentName}");
                        componentProps = null;
                        break;

                }
                if (componentProps != null)
                {
                    componentProps.SetAttribute("visible", component.GetType().IsVisible.ToString().ToLower());
                    gameObject.AppendChild(componentProps);
                }
            }
        }
    }
}